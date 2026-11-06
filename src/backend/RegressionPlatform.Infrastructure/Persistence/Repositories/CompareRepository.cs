using System.Text;
using Dapper;
using RegressionPlatform.Application.Abstractions;
using RegressionPlatform.Application.Features.Compare;
using RegressionPlatform.Application.Features.Compare.Contracts;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Infrastructure.Persistence.Sql;
using RegressionPlatform.Infrastructure.Services;
using RegressionPlatform.Shared.Pagination;
using DomainEntities = RegressionPlatform.Domain.Entities;

namespace RegressionPlatform.Infrastructure.Persistence.Repositories;

/// <summary>Dapper query-side data access for compares (History + detail screens).</summary>
public sealed class CompareRepository(IDbConnectionFactory connectionFactory) : ICompareRepository
{
    private sealed record IgnoreRow(int RuleId, string JsonPath, int? ApiId);

    /// <summary>Key-sorted canonical form of a stored body (null / non-JSON pass through).</summary>
    private static string? SortKeys(string? body) => body is null ? null : ResponseCanonicalizer.Process(body, []).Canonical;

    /// <summary>The body with ignore rules applied, keeping the display's key order.</summary>
    private static string Compared(
        string body,
        IReadOnlyList<string> ignorePaths,
        IReadOnlyList<ResponseCanonicalizer.ValueTrim> trims,
        bool ignoreOrder
    ) =>
        ignoreOrder
            ? ResponseCanonicalizer.Process(body, ignorePaths, trims).Canonical
            : ResponseCanonicalizer.ProcessOrdered(body, ignorePaths, trims).Canonical;

    private sealed record TrimRow(int RuleId, string JsonPath, string Delimiter, int? ApiId);

    // The diff-detail row plus the request's ApiId (needed to pick which ignore rules apply).
    private sealed record DetailDiffRow(
        long RequestId,
        string Title,
        string Method,
        string Endpoint,
        string Result,
        string? DiffJson,
        int? StatusCodeA,
        int? StatusCodeB,
        string? ResponseAJson,
        string? ResponseBJson,
        string? ErrorMessageA,
        string? ErrorMessageB,
        int ApiId,
        bool IsEditedA,
        bool IsEditedB
    );

    /// <summary>
    /// Builds a LIKE pattern (ESCAPE '\') that matches a diff entry with the given change type at the
    /// given index-normalized path. Diff entries are compact JSON where "path" precedes "changeType",
    /// e.g. {"path":"$.a[3].b","changeType":"ValueChanged",...}; each <c>[*]</c> in the normalized path
    /// matches any array index. Returns null when either input is missing (no filter).
    /// </summary>
    private static string? BuildDiffLike(string? changeType, string? path)
    {
        if (string.IsNullOrWhiteSpace(changeType) || string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var builder = new StringBuilder("%\"path\":\"");
        var segments = path.Split("[*]");
        for (var i = 0; i < segments.Length; i++)
        {
            builder.Append(EscapeLike(segments[i]));
            if (i < segments.Length - 1)
            {
                builder.Append("\\[%]"); // literal '[' + any index + literal ']'
            }
        }
        builder.Append("\",\"changeType\":\"").Append(EscapeLike(changeType)).Append("\"%");
        return builder.ToString();
    }

    /// <summary>Escapes LIKE metacharacters (%, _, [) and the escape char, for use with ESCAPE '\'.</summary>
    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");

    // Groups rule rows into per-rule paths, keeping only those that apply to the given API.
    private static List<string> ApplicablePaths(IEnumerable<IgnoreRow> rows, int apiId) =>
        rows.GroupBy(rule => new { rule.RuleId, rule.JsonPath })
            .Select(group => new IgnoreRulePath(
                group.Key.JsonPath,
                group.Select(rule => rule.ApiId).Where(id => id.HasValue).Select(id => id!.Value).ToList()
            ))
            .Where(rule => rule.AppliesTo(apiId))
            .Select(rule => rule.JsonPath)
            .ToList();

    // Whitelist mapping detail-list sort keys -> SQL columns (guards ORDER BY against injection).
    private static readonly IReadOnlyDictionary<string, string> DetailSortColumns = new Dictionary<string, string>(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["title"] = "req.Title",
        ["endpoint"] = "a.Endpoint",
        ["result"] = "cd.Result",
    };

    public async Task<long> CreateJobAsync(
        DomainEntities.CompareJob job,
        IReadOnlyList<int> apiIds,
        CancellationToken cancellationToken
    )
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var id = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                CompareSql.InsertJob,
                new
                {
                    job.ProjectId,
                    job.RunAId,
                    job.RunBId,
                    job.Status,
                    job.BaselineMode,
                    job.IgnoreOrder,
                },
                transaction,
                cancellationToken: cancellationToken
            )
        );

        var distinctApiIds = apiIds.Distinct().ToList();
        if (distinctApiIds.Count > 0)
        {
            var rows = distinctApiIds.Select(apiId => new { CompareJobId = id, ApiId = apiId });
            await connection.ExecuteAsync(
                new CommandDefinition(CompareSql.InsertJobApi, rows, transaction, cancellationToken: cancellationToken)
            );
        }

        transaction.Commit();
        return id;
    }

    public async Task<IReadOnlyList<CompareJobListItem>> GetJobsByProjectAsync(int projectId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<CompareJobListItem>(
            new CommandDefinition(CompareSql.GetJobsByProject, new { ProjectId = projectId }, cancellationToken: cancellationToken)
        );
        return rows.ToList();
    }

    public async Task<DomainEntities.CompareJob?> GetJobAsync(long jobId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<DomainEntities.CompareJob>(
            new CommandDefinition(CompareSql.GetJobEntity, new { Id = jobId }, cancellationToken: cancellationToken)
        );
    }

    public async Task<bool> TryStartAsync(long jobId, DateTime startedDate, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(
                CompareSql.TryStart,
                new { Id = jobId, StartedDate = startedDate },
                cancellationToken: cancellationToken
            )
        );
        return rows > 0;
    }

    public async Task<bool> MarkCancelledAsync(long jobId, DateTime finishedDate, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(CompareSql.MarkCancelled, new { Id = jobId, FinishedDate = finishedDate }, cancellationToken: cancellationToken)
        );
        return rows > 0;
    }

    public async Task<bool> HasReportAsync(long jobId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(CompareSql.HasReport, new { Id = jobId }, cancellationToken: cancellationToken)
        );
    }

    public async Task DeleteAsync(long jobId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        await connection.ExecuteAsync(
            new CommandDefinition(CompareSql.Delete, new { Id = jobId }, transaction, cancellationToken: cancellationToken)
        );
        transaction.Commit();
    }

    public async Task<CompareSummaryResponse?> GetSummaryAsync(long jobId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<CompareSummaryResponse>(
            new CommandDefinition(CompareSql.GetSummary, new { Id = jobId }, cancellationToken: cancellationToken)
        );
    }

    public async Task<IReadOnlyList<CompareApiStatistic>> GetApiStatisticsAsync(long compareResultId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<CompareApiStatistic>(
            new CommandDefinition(CompareSql.ApiStatistics, new { CompareResultId = compareResultId }, cancellationToken: cancellationToken)
        );
        return rows.ToList();
    }

    public async Task<IReadOnlyList<CompareRequestDiff>> GetDiffsAsync(long compareResultId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<CompareRequestDiff>(
            new CommandDefinition(CompareSql.GetDiffs, new { CompareResultId = compareResultId }, cancellationToken: cancellationToken)
        );
        return rows.ToList();
    }

    public async Task<IReadOnlyList<CompareFullDetailItem>> GetFullDetailsAsync(
        long compareResultId,
        CancellationToken cancellationToken
    )
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<CompareFullDetailItem>(
            new CommandDefinition(CompareSql.FullDetails, new { CompareResultId = compareResultId }, cancellationToken: cancellationToken)
        );
        return rows.ToList();
    }

    public async Task<IReadOnlyList<CompareTestResultItem>> GetTestResultsAsync(
        long compareResultId,
        long runAId,
        long runBId,
        CancellationToken cancellationToken
    )
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<CompareTestResultItem>(
            new CommandDefinition(
                CompareSql.TestResults,
                new { CompareResultId = compareResultId, RunAId = runAId, RunBId = runBId },
                cancellationToken: cancellationToken
            )
        );
        return rows.ToList();
    }

    public async Task<PagedResult<CompareDetailListItem>> GetDetailsPagedAsync(
        CompareDetailListParameters parameters,
        CancellationToken cancellationToken
    )
    {
        using var connection = connectionFactory.CreateConnection();
        var orderByColumn = DetailSortColumns.TryGetValue(parameters.SortBy ?? string.Empty, out var column)
            ? column
            : "cd.RequestId";
        var direction = string.Equals(parameters.SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
            ? "DESC"
            : "ASC";

        var sql = CompareSql.DetailsCount + "\n" + CompareSql.DetailsPaged(orderByColumn, direction);
        var queryParams = new
        {
            parameters.CompareResultId,
            Search = SqlSearch.Contains(parameters.Search),
            Result = string.IsNullOrWhiteSpace(parameters.Result) ? null : parameters.Result,
            parameters.ApiId,
            DiffLike = BuildDiffLike(parameters.ChangeType, parameters.ChangePath),
            Offset = (parameters.PageNumber - 1) * parameters.PageSize,
            parameters.PageSize,
        };

        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, queryParams, cancellationToken: cancellationToken)
        );
        var totalCount = await multi.ReadFirstAsync<int>();
        var items = (await multi.ReadAsync<CompareDetailListItem>()).ToList();
        return new PagedResult<CompareDetailListItem>(items, parameters.PageNumber, parameters.PageSize, totalCount);
    }

    public async Task<CompareDetailDiffResponse?> GetDetailDiffAsync(
        int projectId,
        long compareResultId,
        long runAId,
        long runBId,
        long requestId,
        bool ignoreOrder,
        CancellationToken cancellationToken
    )
    {
        using var connection = connectionFactory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<DetailDiffRow>(
            new CommandDefinition(
                CompareSql.DetailDiff,
                new
                {
                    CompareResultId = compareResultId,
                    RunAId = runAId,
                    RunBId = runBId,
                    RequestId = requestId,
                },
                cancellationToken: cancellationToken
            )
        );
        if (row is null)
        {
            return null;
        }

        // Apply the current ignore + extract-root rules (scoped to this request's API) to both bodies,
        // so the side-by-side view shows the same data the compare engine actually diffed.
        var ignoreRows = await connection.QueryAsync<IgnoreRow>(
            new CommandDefinition(CompareSql.IgnorePaths, new { ProjectId = projectId }, cancellationToken: cancellationToken)
        );
        var applicable = ApplicablePaths(ignoreRows, row.ApiId);

        var extractRows = await connection.QueryAsync<IgnoreRow>(
            new CommandDefinition(CompareSql.ExtractRootPaths, new { ProjectId = projectId }, cancellationToken: cancellationToken)
        );
        var applicableExtract = ApplicablePaths(extractRows, row.ApiId);

        var trimRows = await connection.QueryAsync<TrimRow>(
            new CommandDefinition(CompareSql.TrimRules, new { ProjectId = projectId }, cancellationToken: cancellationToken)
        );
        var applicableTrims = trimRows
            .GroupBy(trim => trim.RuleId)
            .Where(group => group.All(trim => trim.ApiId is null) || group.Any(trim => trim.ApiId == row.ApiId))
            .Select(group => new ResponseCanonicalizer.ValueTrim(group.First().JsonPath, group.First().Delimiter))
            .ToList();

        // Bodies are stored in their original key order. Ignore-order compares show them key-sorted
        // (exactly what the engine diffed — the previous display); strict-order compares show them as
        // returned. Then unwrap the envelope (if any extract-root rule applies) so both sides line up.
        var bodyA = ignoreOrder ? SortKeys(row.ResponseAJson) : row.ResponseAJson;
        var bodyB = ignoreOrder ? SortKeys(row.ResponseBJson) : row.ResponseBJson;
        bodyA = applicableExtract.Count == 0 || bodyA is null ? bodyA : ResponseCanonicalizer.ExtractRoot(bodyA, applicableExtract);
        bodyB = applicableExtract.Count == 0 || bodyB is null ? bodyB : ResponseCanonicalizer.ExtractRoot(bodyB, applicableExtract);

        // DISPLAY alignment (ignore-order only): reorder B's keyed arrays to A's element order (same
        // identity matching the diff engine uses), so the side-by-side shows the same item opposite
        // itself. Strict order keeps B as returned — a moved item IS the difference there.
        if (ignoreOrder && bodyA is not null && bodyB is not null)
        {
            bodyB = JsonDiffer.AlignArraysForDisplay(bodyA, bodyB);
        }

        // Full (unwrapped) bodies are shown; the ignore-applied bodies let the client mark which lines
        // are ignored (so they render distinctly and stay out of the change count/navigation). Both use
        // the same key order as the displayed body so the line match stays exact.
        var comparedA = bodyA is null ? null : Compared(bodyA, applicable, applicableTrims, ignoreOrder);
        var comparedB = bodyB is null ? null : Compared(bodyB, applicable, applicableTrims, ignoreOrder);

        return new CompareDetailDiffResponse(
            row.RequestId,
            row.Title,
            row.Method,
            row.Endpoint,
            row.Result,
            row.DiffJson,
            row.StatusCodeA,
            row.StatusCodeB,
            bodyA,
            bodyB,
            comparedA,
            comparedB,
            row.ErrorMessageA,
            row.ErrorMessageB,
            row.IsEditedA,
            row.IsEditedB
        );
    }
}
