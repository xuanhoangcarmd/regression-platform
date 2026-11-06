using Dapper;
using RegressionPlatform.Application.Abstractions;
using RegressionPlatform.Application.Features.Runs;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Infrastructure.Persistence.Sql;
using RegressionPlatform.Shared.Pagination;
using DomainEntities = RegressionPlatform.Domain.Entities;

namespace RegressionPlatform.Infrastructure.Persistence.Repositories;

/// <summary>Dapper data access for regression runs.</summary>
public sealed class RunRepository(IDbConnectionFactory connectionFactory) : IRunRepository
{
    // Whitelist mapping response-list sort keys -> SQL columns (guards ORDER BY against injection).
    private static readonly IReadOnlyDictionary<string, string> ResponseSortColumns = new Dictionary<string, string>(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["title"] = "req.Title",
        ["endpoint"] = "a.Endpoint",
        ["status"] = "Status",
        ["statusCode"] = "rr.StatusCode",
        ["durationMs"] = "rr.DurationMs",
        ["responseSize"] = "rr.ResponseSize",
        ["createdDate"] = "rr.CreatedDate",
    };

    public async Task<IReadOnlyList<RunResponse>> GetByProjectAsync(int projectId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var runs = await connection.QueryAsync<RunResponse>(
            new CommandDefinition(RunSql.GetByProject, new { ProjectId = projectId }, cancellationToken: cancellationToken)
        );
        return runs.ToList();
    }

    public async Task<IReadOnlyList<RunProgressResponse>> GetProgressByIdsAsync(
        int projectId,
        IReadOnlyList<long> runIds,
        CancellationToken cancellationToken
    )
    {
        if (runIds.Count == 0)
        {
            return [];
        }

        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<RunProgressResponse>(
            new CommandDefinition(
                RunSql.GetProgressByIds,
                new { ProjectId = projectId, Ids = runIds },
                cancellationToken: cancellationToken
            )
        );
        return rows.ToList();
    }

    public async Task<RunResponse?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<RunResponse>(
            new CommandDefinition(RunSql.GetById, new { Id = id }, cancellationToken: cancellationToken)
        );
    }

    public async Task<DomainEntities.Run?> GetEntityAsync(long id, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<DomainEntities.Run>(
            new CommandDefinition(RunSql.GetEntityById, new { Id = id }, cancellationToken: cancellationToken)
        );
    }

    public async Task<IReadOnlyList<ApiRunStatistic>> GetApiStatisticsAsync(long runId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var stats = await connection.QueryAsync<ApiRunStatistic>(
            new CommandDefinition(RunSql.ApiStatistics, new { RunId = runId }, cancellationToken: cancellationToken)
        );
        return stats.ToList();
    }

    public async Task<PagedResult<RunResponseListItem>> GetResponsesPagedAsync(
        RunResponseListParameters parameters,
        CancellationToken cancellationToken
    )
    {
        using var connection = connectionFactory.CreateConnection();
        var orderByColumn = ResponseSortColumns.TryGetValue(parameters.SortBy ?? string.Empty, out var column)
            ? column
            : "rr.RequestId";
        var direction = string.Equals(parameters.SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
            ? "DESC"
            : "ASC";
        var sql = RunSql.ResponsesCount + "\n" + RunSql.ResponsesPaged(orderByColumn, direction);
        var queryParams = new
        {
            parameters.RunId,
            Search = SqlSearch.Contains(parameters.Search),
            Status = string.IsNullOrWhiteSpace(parameters.Status) ? null : parameters.Status,
            parameters.ApiId,
            Offset = (parameters.PageNumber - 1) * parameters.PageSize,
            parameters.PageSize,
        };

        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, queryParams, cancellationToken: cancellationToken)
        );
        var totalCount = await multi.ReadFirstAsync<int>();
        var items = (await multi.ReadAsync<RunResponseListItem>()).ToList();
        return new PagedResult<RunResponseListItem>(items, parameters.PageNumber, parameters.PageSize, totalCount);
    }

    public async Task<IReadOnlyList<RunResponseExportRow>> GetResponsesForExportAsync(
        long runId,
        CancellationToken cancellationToken
    )
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<RunResponseExportRow>(
            new CommandDefinition(RunSql.ResponseExport, new { RunId = runId }, cancellationToken: cancellationToken)
        );
        return rows.ToList();
    }

    public async Task<RunResponseDetail?> GetResponseDetailAsync(
        long runId,
        long requestId,
        CancellationToken cancellationToken
    )
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<RunResponseDetail>(
            new CommandDefinition(
                RunSql.ResponseDetail,
                new { RunId = runId, RequestId = requestId },
                cancellationToken: cancellationToken
            )
        );
    }

    public async Task<bool> MarkQueuedAsync(long id, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(RunSql.MarkQueued, new { Id = id }, cancellationToken: cancellationToken)
        );
        return rows > 0;
    }

    public async Task<bool> MarkCancelledAsync(long id, DateTime endTime, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(RunSql.MarkCancelled, new { Id = id, EndTime = endTime }, cancellationToken: cancellationToken)
        );
        return rows > 0;
    }

    public async Task<IReadOnlyList<int>> GetApiIdsAsync(long runId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var ids = await connection.QueryAsync<int>(
            new CommandDefinition(RunSql.GetApiIds, new { RunId = runId }, cancellationToken: cancellationToken)
        );
        return ids.ToList();
    }

    public async Task<bool> UpdateAsync(
        DomainEntities.Run run,
        IReadOnlyList<int> apiIds,
        CancellationToken cancellationToken
    )
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(
                RunSql.Update,
                new
                {
                    run.Id,
                    run.DatasetId,
                    run.Title,
                    run.Description,
                    run.Environment,
                    run.RunType,
                    run.RequestLimitPerApi,
                    run.MaxParallelApis,
                    run.MaxParallelRequestsPerApi,
                },
                transaction,
                cancellationToken: cancellationToken
            )
        );

        if (rows == 0)
        {
            transaction.Rollback();
            return false; // not Pending — nothing updated.
        }

        // Replace the API links (empty = all of the dataset's APIs).
        await connection.ExecuteAsync(
            new CommandDefinition(RunSql.DeleteApis, new { RunId = run.Id }, transaction, cancellationToken: cancellationToken)
        );
        var distinctApiIds = apiIds.Distinct().ToList();
        if (distinctApiIds.Count > 0)
        {
            var apiRows = distinctApiIds.Select(apiId => new { RunId = run.Id, ApiId = apiId });
            await connection.ExecuteAsync(
                new CommandDefinition(RunSql.InsertApi, apiRows, transaction, cancellationToken: cancellationToken)
            );
        }

        transaction.Commit();
        return true;
    }

    public async Task<bool> HasCompareAsync(long id, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var flag = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(RunSql.HasCompare, new { Id = id }, cancellationToken: cancellationToken)
        );
        return flag != 0;
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        await connection.ExecuteAsync(
            new CommandDefinition(RunSql.Delete, new { Id = id }, transaction, cancellationToken: cancellationToken)
        );
        transaction.Commit();
    }

    public async Task<long> CreateAsync(
        DomainEntities.Run run,
        IReadOnlyList<int> apiIds,
        CancellationToken cancellationToken
    )
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var id = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                RunSql.Insert,
                new
                {
                    run.ProjectId,
                    run.DatasetId,
                    run.Title,
                    run.Description,
                    run.Environment,
                    run.RunType,
                    run.Status,
                    run.RequestLimitPerApi,
                    run.MaxParallelApis,
                    run.MaxParallelRequestsPerApi,
                    run.TotalRequest,
                },
                transaction,
                cancellationToken: cancellationToken
            )
        );

        var distinctApiIds = apiIds.Distinct().ToList();
        if (distinctApiIds.Count > 0)
        {
            var rows = distinctApiIds.Select(apiId => new { RunId = id, ApiId = apiId });
            await connection.ExecuteAsync(
                new CommandDefinition(RunSql.InsertApi, rows, transaction, cancellationToken: cancellationToken)
            );
        }

        transaction.Commit();
        return id;
    }
}
