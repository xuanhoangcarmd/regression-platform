using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using RegressionPlatform.Application.Abstractions;
using RegressionPlatform.Application.Features.Compare;
using RegressionPlatform.Application.Features.Compare.Contracts;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Infrastructure.Persistence.Sql;
using DomainEntities = RegressionPlatform.Domain.Entities;

namespace RegressionPlatform.Infrastructure.Persistence.Repositories;

/// <summary>Dapper data access for the compare engine (background worker).</summary>
public sealed class CompareExecutionRepository(IDbConnectionFactory connectionFactory) : ICompareExecutionRepository
{
    private sealed record BodiesRow(long RequestId, string? JsonA, string? JsonB);

    /// <summary>Ids per IN (...) query — well under SQL Server's 2100-parameter limit.</summary>
    private const int BodyChunkSize = 500;

    private sealed record IgnoreRow(int RuleId, string JsonPath, int? ApiId);

    private sealed record TrimRow(int RuleId, string JsonPath, string Delimiter, int? ApiId);

    public async Task<IReadOnlyList<TrimRulePath>> GetTrimRulesAsync(int projectId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<TrimRow>(
            new CommandDefinition(CompareSql.TrimRules, new { ProjectId = projectId }, cancellationToken: cancellationToken)
        );
        return rows.GroupBy(row => new { row.RuleId, row.JsonPath, row.Delimiter })
            .Select(group => new TrimRulePath(
                group.Key.JsonPath,
                group.Key.Delimiter,
                group.Select(row => row.ApiId).Where(id => id.HasValue).Select(id => id!.Value).ToList()
            ))
            .ToList();
    }

    public async Task<CompareExecutionInfo?> GetExecutionInfoAsync(long jobId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<CompareExecutionInfo>(
            new CommandDefinition(CompareSql.GetExecutionInfo, new { Id = jobId }, cancellationToken: cancellationToken)
        );
    }

    public async Task<IReadOnlyList<int>> GetScopeApiIdsAsync(long jobId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var ids = await connection.QueryAsync<int>(
            new CommandDefinition(CompareSql.GetScopeApiIds, new { JobId = jobId }, cancellationToken: cancellationToken)
        );
        return ids.ToList();
    }

    public async Task<IReadOnlyList<IgnoreRulePath>> GetIgnorePathsAsync(int projectId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<IgnoreRow>(
            new CommandDefinition(CompareSql.IgnorePaths, new { ProjectId = projectId }, cancellationToken: cancellationToken)
        );
        return GroupRulePaths(rows);
    }

    public async Task<IReadOnlyList<IgnoreRulePath>> GetExtractRootPathsAsync(int projectId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<IgnoreRow>(
            new CommandDefinition(CompareSql.ExtractRootPaths, new { ProjectId = projectId }, cancellationToken: cancellationToken)
        );
        return GroupRulePaths(rows);
    }

    // One IgnoreRulePath per rule; its ApiIds = the non-null linked APIs (empty = applies to all).
    private static IReadOnlyList<IgnoreRulePath> GroupRulePaths(IEnumerable<IgnoreRow> rows) =>
        rows.GroupBy(row => new { row.RuleId, row.JsonPath })
            .Select(group => new IgnoreRulePath(
                group.Key.JsonPath,
                group.Select(row => row.ApiId).Where(id => id.HasValue).Select(id => id!.Value).ToList()
            ))
            .ToList();

    public async Task<IReadOnlyList<CompareResponseSummary>> GetResponseSummariesAsync(
        long runId,
        IReadOnlyList<int> apiIds,
        bool useExpected,
        CancellationToken cancellationToken
    )
    {
        using var connection = connectionFactory.CreateConnection();
        var (sql, parameters) =
            apiIds.Count == 0
                ? (useExpected ? CompareSql.ExpectedSummariesAll : CompareSql.ResponseSummariesAll, (object)new { RunId = runId })
                : (
                    useExpected ? CompareSql.ExpectedSummariesForApis : CompareSql.ResponseSummariesForApis,
                    new { RunId = runId, ApiIds = apiIds }
                );

        var rows = await connection.QueryAsync<CompareResponseSummary>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)
        );
        return rows.ToList();
    }

    public async Task<IReadOnlyDictionary<long, (string? JsonA, string? JsonB)>> GetResponseBodiesAsync(
        long runAId,
        long runBId,
        IReadOnlyCollection<long> requestIds,
        bool useExpected,
        CancellationToken cancellationToken
    )
    {
        var bodies = new Dictionary<long, (string? JsonA, string? JsonB)>(requestIds.Count);
        var sql = useExpected ? CompareSql.GetExpectedResponseBodies : CompareSql.GetResponseBodies;
        using var connection = connectionFactory.CreateConnection();
        // One round trip per chunk instead of one per changed request.
        foreach (var chunk in requestIds.Chunk(BodyChunkSize))
        {
            var rows = await connection.QueryAsync<BodiesRow>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        RunAId = runAId,
                        RunBId = runBId,
                        RequestIds = chunk,
                    },
                    cancellationToken: cancellationToken
                )
            );
            foreach (var row in rows)
            {
                bodies[row.RequestId] = (row.JsonA, row.JsonB);
            }
        }
        return bodies;
    }

    public async Task<bool> MarkRunningAsync(long jobId, DateTime startedDate, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(CompareSql.MarkRunning, new { Id = jobId, StartedDate = startedDate }, cancellationToken: cancellationToken)
        );
        return rows > 0;
    }

    public async Task UpdateProgressAsync(long jobId, int progress, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            new CommandDefinition(CompareSql.UpdateProgress, new { Id = jobId, Progress = progress }, cancellationToken: cancellationToken)
        );
    }

    public async Task<long> SaveResultAsync(
        DomainEntities.CompareResult result,
        IReadOnlyList<DomainEntities.CompareDetail> details,
        CancellationToken cancellationToken
    )
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var resultId = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                CompareSql.InsertResult,
                new
                {
                    result.RunAId,
                    result.RunBId,
                    result.TotalRequest,
                    result.UnchangedCount,
                    result.ChangedCount,
                    result.FailedCount,
                    result.AddedCount,
                    result.RemovedCount,
                    result.ModifiedCount,
                },
                transaction,
                cancellationToken: cancellationToken
            )
        );

        if (details.Count > 0)
        {
            // SqlBulkCopy inside the same transaction: one streamed insert instead of one round trip per row.
            var table = new DataTable();
            table.Columns.Add("CompareResultId", typeof(long));
            table.Columns.Add("RequestId", typeof(long));
            table.Columns.Add("Result", typeof(string));
            table.Columns.Add("DiffJson", typeof(string));
            table.Columns.Add("IsEdited", typeof(bool));
            table.Columns.Add("CreatedDate", typeof(DateTime));
            var createdDate = DateTime.UtcNow;
            foreach (var detail in details)
            {
                table.Rows.Add(resultId, detail.RequestId, detail.Result, (object?)detail.DiffJson ?? DBNull.Value, detail.IsEdited, createdDate);
            }

            using var bulkCopy = new SqlBulkCopy((SqlConnection)connection, SqlBulkCopyOptions.Default, (SqlTransaction)transaction)
            {
                DestinationTableName = CompareSql.DetailTable,
                BatchSize = 5000,
                BulkCopyTimeout = 0,
            };
            foreach (DataColumn column in table.Columns)
            {
                bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
            }
            await bulkCopy.WriteToServerAsync(table, cancellationToken);
        }

        transaction.Commit();
        return resultId;
    }

    public async Task CompleteJobAsync(long jobId, long compareResultId, DateTime finishedDate, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            new CommandDefinition(
                CompareSql.CompleteJob,
                new
                {
                    Id = jobId,
                    CompareResultId = compareResultId,
                    FinishedDate = finishedDate,
                },
                cancellationToken: cancellationToken
            )
        );
    }

    public async Task MarkJobFailedAsync(long jobId, DateTime finishedDate, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            new CommandDefinition(CompareSql.MarkJobFailed, new { Id = jobId, FinishedDate = finishedDate }, cancellationToken: cancellationToken)
        );
    }
}
