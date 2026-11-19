using Dapper;
using RegressionPlatform.Application.Abstractions;
using RegressionPlatform.Application.Features.Runs;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Infrastructure.Persistence.Sql;
using DomainEntities = RegressionPlatform.Domain.Entities;

namespace RegressionPlatform.Infrastructure.Persistence.Repositories;

/// <summary>Dapper data access for the run executor.</summary>
public sealed class RunExecutionRepository(IDbConnectionFactory connectionFactory, IRunExecutorIdentity executorIdentity)
    : IRunExecutionRepository
{
    private sealed record IgnoreRow(int RuleId, string JsonPath, int? ApiId);

    public async Task<RunExecutionInfo?> GetExecutionInfoAsync(long runId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<RunExecutionInfo>(
            new CommandDefinition(RunExecutionSql.GetExecutionInfo, new { Id = runId }, cancellationToken: cancellationToken)
        );
    }

    public async Task<IReadOnlyList<int>> GetTargetApiIdsAsync(long runId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var ids = await connection.QueryAsync<int>(
            new CommandDefinition(RunExecutionSql.GetTargetApiIds, new { RunId = runId }, cancellationToken: cancellationToken)
        );
        return ids.ToList();
    }

    public async Task<IReadOnlyList<RunnableRequest>> GetRunnableRequestsAsync(
        int datasetId,
        IReadOnlyList<int> apiIds,
        CancellationToken cancellationToken
    )
    {
        using var connection = connectionFactory.CreateConnection();
        var (sql, parameters) =
            apiIds.Count == 0
                ? (RunExecutionSql.RunnableAll, (object)new { DatasetId = datasetId })
                : (RunExecutionSql.RunnableForApis, new { DatasetId = datasetId, ApiIds = apiIds });

        var requests = await connection.QueryAsync<RunnableRequest>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)
        );
        return requests.ToList();
    }

    public async Task<IReadOnlyList<IgnoreRulePath>> GetIgnorePathsAsync(int projectId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<IgnoreRow>(
            new CommandDefinition(RunExecutionSql.IgnorePaths, new { ProjectId = projectId }, cancellationToken: cancellationToken)
        );

        // One IgnoreRulePath per rule; its ApiIds = the non-null linked APIs (empty = applies to all).
        return rows.GroupBy(row => new { row.RuleId, row.JsonPath })
            .Select(group => new IgnoreRulePath(
                group.Key.JsonPath,
                group.Select(row => row.ApiId).Where(id => id.HasValue).Select(id => id!.Value).ToList()
            ))
            .ToList();
    }

    public async Task<ResponseInsertResult> InsertResponseAsync(DomainEntities.RunResponse response, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var outcome = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                RunExecutionSql.InsertOwnedResponse,
                new
                {
                    response.RunId,
                    response.RequestId,
                    response.StatusCode,
                    response.DurationMs,
                    response.ResponseHash,
                    response.ResponseOrderedHash,
                    response.ResponseJson,
                    response.ResponseHeaders,
                    response.ResponseSize,
                    response.ErrorMessage,
                    Instance = executorIdentity.InstanceId,
                },
                cancellationToken: cancellationToken
            )
        );
        return outcome switch
        {
            < 0 => ResponseInsertResult.NotOwner,
            0 => ResponseInsertResult.Duplicate,
            _ => ResponseInsertResult.Inserted,
        };
    }

    public async Task<bool> HeartbeatAsync(long runId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(
                RunExecutionSql.Heartbeat,
                new { Id = runId, Instance = executorIdentity.InstanceId },
                cancellationToken: cancellationToken
            )
        );
        return rows > 0;
    }

    public async Task<RunnableRequest?> GetRunnableRequestAsync(long requestId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<RunnableRequest>(
            new CommandDefinition(RunExecutionSql.RunnableById, new { RequestId = requestId }, cancellationToken: cancellationToken)
        );
    }

    public async Task ReplaceResponseAsync(DomainEntities.RunResponse response, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        await connection.ExecuteAsync(
            new CommandDefinition(
                RunExecutionSql.DeleteResponse,
                new { response.RunId, response.RequestId },
                transaction,
                cancellationToken: cancellationToken
            )
        );
        await connection.ExecuteAsync(
            new CommandDefinition(RunExecutionSql.InsertResponse, ResponseParams(response), transaction, cancellationToken: cancellationToken)
        );
        transaction.Commit();
    }

    public async Task RecalculateRunCountsAsync(long runId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            new CommandDefinition(RunExecutionSql.RecalculateCounts, new { Id = runId }, cancellationToken: cancellationToken)
        );
    }

    private static object ResponseParams(DomainEntities.RunResponse response) =>
        new
        {
            response.RunId,
            response.RequestId,
            response.StatusCode,
            response.DurationMs,
            response.ResponseHash,
            response.ResponseOrderedHash,
            response.ResponseJson,
            response.ResponseHeaders,
            response.ResponseSize,
            response.ErrorMessage,
        };

    public async Task<bool> TryMarkRunningAsync(long runId, DateTime startTime, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(
                RunExecutionSql.MarkRunning,
                new { Id = runId, StartTime = startTime, Instance = executorIdentity.InstanceId },
                cancellationToken: cancellationToken
            )
        );
        return rows > 0;
    }

    public async Task<int> FailStaleRunsAsync(DateTime endTime, TimeSpan staleAfter, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(
            new CommandDefinition(
                RunExecutionSql.FailStale,
                new { EndTime = endTime, StaleSeconds = (int)staleAfter.TotalSeconds },
                cancellationToken: cancellationToken
            )
        );
    }

    public async Task<IReadOnlyList<long>> GetQueuedRunIdsAsync(CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var ids = await connection.QueryAsync<long>(
            new CommandDefinition(
                RunExecutionSql.GetQueuedRunIds,
                new { NodePrefix = executorIdentity.NodeName + "|" },
                cancellationToken: cancellationToken
            )
        );
        return ids.ToList();
    }

    public async Task CompleteRunAsync(
        long runId,
        string status,
        DateTime endTime,
        long durationMs,
        int totalRequest,
        int passCount,
        int warningCount,
        int failedCount,
        CancellationToken cancellationToken
    )
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            new CommandDefinition(
                RunExecutionSql.Complete,
                new
                {
                    Id = runId,
                    Status = status,
                    EndTime = endTime,
                    DurationMs = durationMs,
                    TotalRequest = totalRequest,
                    PassCount = passCount,
                    WarningCount = warningCount,
                    FailedCount = failedCount,
                    Instance = executorIdentity.InstanceId,
                },
                cancellationToken: cancellationToken
            )
        );
    }
}
