using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using RegressionPlatform.Application.Abstractions;
using RegressionPlatform.Application.Features.Runs;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Infrastructure.Persistence.Sql;
using RegressionPlatform.Infrastructure.Services;

namespace RegressionPlatform.Infrastructure.Persistence.Repositories;

/// <summary>Dapper data access for a run's expected (edited) responses.</summary>
public sealed class ExpectedResponseRepository(IDbConnectionFactory connectionFactory) : IExpectedResponseRepository
{
    /// <summary>Ids per IN (...) query — well under SQL Server's 2100-parameter limit.</summary>
    private const int ChunkSize = 500;

    private sealed record DetailRow(
        long RequestId,
        int ApiId,
        string Title,
        int? StatusCode,
        string? ResponseJson,
        string? ErrorMessage,
        long? OverrideId,
        int? OverrideStatusCode,
        string? OverrideResponseJson,
        string? OverrideReason,
        string? OverrideUpdatedBy,
        DateTime? OverrideUpdatedDate
    );

    public async Task<IReadOnlyList<ExpectedApiItem>> GetApisAsync(long runId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<ExpectedApiItem>(
            new CommandDefinition(ExpectedResponseSql.Apis, new { RunId = runId }, cancellationToken: cancellationToken)
        );
        return rows.ToList();
    }

    public async Task<IReadOnlyList<ExpectedRequestItem>> GetRequestsAsync(long runId, int apiId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<ExpectedRequestItem>(
            new CommandDefinition(ExpectedResponseSql.Requests, new { RunId = runId, ApiId = apiId }, cancellationToken: cancellationToken)
        );
        return rows.ToList();
    }

    public async Task<IReadOnlyList<ExpectedResponseDetail>> GetDetailsAsync(
        long runId,
        IReadOnlyCollection<long> requestIds,
        CancellationToken cancellationToken
    )
    {
        var details = new List<ExpectedResponseDetail>(requestIds.Count);
        using var connection = connectionFactory.CreateConnection();
        foreach (var chunk in requestIds.Chunk(ChunkSize))
        {
            var rows = await connection.QueryAsync<DetailRow>(
                new CommandDefinition(
                    ExpectedResponseSql.Details,
                    new { RunId = runId, RequestIds = chunk },
                    cancellationToken: cancellationToken
                )
            );
            details.AddRange(rows.Select(ToDetail));
        }
        return details;
    }

    public async Task<int> CountResponsesAsync(long runId, IReadOnlyCollection<long> requestIds, CancellationToken cancellationToken)
    {
        var count = 0;
        using var connection = connectionFactory.CreateConnection();
        foreach (var chunk in requestIds.Distinct().Chunk(ChunkSize))
        {
            count += await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    ExpectedResponseSql.CountResponses,
                    new { RunId = runId, RequestIds = chunk },
                    cancellationToken: cancellationToken
                )
            );
        }
        return count;
    }

    public async Task<int> SaveAsync(
        long runId,
        IReadOnlyList<ExpectedResponseSaveItem> items,
        string? userName,
        CancellationToken cancellationToken
    )
    {
        if (items.Count == 0)
        {
            return 0;
        }

        // Canonicalize + hash exactly like a capture (RunExecutor) so the compare engine's hash fast
        // path treats an edited body that matches Run B as Unchanged without loading either body.
        var staging = new DataTable();
        staging.Columns.Add("RequestId", typeof(long));
        staging.Columns.Add("StatusCode", typeof(int));
        staging.Columns.Add("ResponseJson", typeof(string));
        staging.Columns.Add("ResponseHash", typeof(string));
        staging.Columns.Add("ResponseOrderedHash", typeof(string));
        staging.Columns.Add("Reason", typeof(string));
        foreach (var item in items)
        {
            // Stored in the editor's key order; both hashes like a capture (sorted + original order).
            var canonical = ResponseCanonicalizer.Process(item.ResponseJson, []);
            var original = ResponseCanonicalizer.ProcessOrdered(item.ResponseJson, []);
            staging.Rows.Add(
                item.RequestId,
                (object?)item.StatusCode ?? DBNull.Value,
                original.Canonical,
                canonical.Hash,
                original.Hash,
                string.IsNullOrWhiteSpace(item.Reason) ? DBNull.Value : item.Reason.Trim()
            );
        }

        await using var connection = (SqlConnection)connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        // Session temp table (created in its own batch so it outlives the statement).
        await connection.ExecuteAsync(
            new CommandDefinition(ExpectedResponseSql.CreateStaging, transaction: transaction, cancellationToken: cancellationToken)
        );

        using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
        {
            bulkCopy.DestinationTableName = ExpectedResponseSql.StagingTable;
            foreach (DataColumn column in staging.Columns)
            {
                bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
            }
            await bulkCopy.WriteToServerAsync(staging, cancellationToken);
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                ExpectedResponseSql.Upsert,
                new { RunId = runId, UserName = userName },
                transaction,
                cancellationToken: cancellationToken
            )
        );

        await transaction.CommitAsync(cancellationToken);
        return items.Count;
    }

    public async Task<int> DeleteAsync(long runId, IReadOnlyCollection<long> requestIds, CancellationToken cancellationToken)
    {
        var removed = 0;
        using var connection = connectionFactory.CreateConnection();
        foreach (var chunk in requestIds.Chunk(ChunkSize))
        {
            removed += await connection.ExecuteAsync(
                new CommandDefinition(
                    ExpectedResponseSql.Delete,
                    new { RunId = runId, RequestIds = chunk },
                    cancellationToken: cancellationToken
                )
            );
        }
        return removed;
    }

    private static ExpectedResponseDetail ToDetail(DetailRow row) =>
        new(
            row.RequestId,
            row.ApiId,
            row.Title,
            row.StatusCode,
            row.ResponseJson,
            row.ErrorMessage,
            row.OverrideId is null
                ? null
                : new ExpectedOverride(
                    row.OverrideStatusCode,
                    row.OverrideResponseJson ?? string.Empty,
                    row.OverrideReason,
                    row.OverrideUpdatedBy,
                    row.OverrideUpdatedDate ?? DateTime.UtcNow
                )
        );
}
