namespace RegressionPlatform.Infrastructure.Persistence.Sql;

/// <summary>Parameterized SQL for regression runs (Run + RunApi).</summary>
internal static class RunSql
{
    // NOTE: column order must match the RunResponse record's constructor (Dapper binds positionally).
    // ExecutedRequest + ProgressPercent are tracked in memory (not stored); the query handler fills
    // them for running runs — selected here as NULL so the result shape matches the record.
    private const string Columns =
        "Id, Title, Description, Environment, RunType, Status, RequestLimitPerApi, TotalRequest, "
        + "CAST(NULL AS INT) AS ExecutedRequest, "
        + "PassCount, WarningCount, FailedCount, DurationMs, CreatedDate, "
        + "CAST(NULL AS INT) AS ProgressPercent, "
        // HasCompare: this run is used as baseline/current in a compare (job or result) → not deletable.
        + "CAST(CASE WHEN EXISTS (SELECT 1 FROM CompareJob cj WHERE cj.RunAId = Run.Id OR cj.RunBId = Run.Id) "
        + "            OR EXISTS (SELECT 1 FROM CompareResult cr WHERE cr.RunAId = Run.Id OR cr.RunBId = Run.Id) "
        + "          THEN 1 ELSE 0 END AS BIT) AS HasCompare";

    public const string GetByProject =
        $"SELECT {Columns} FROM Run WHERE ProjectId = @ProjectId ORDER BY CreatedDate DESC, Id DESC;";

    public const string GetById = $"SELECT {Columns} FROM Run WHERE Id = @Id;";

    // Lightweight progress snapshot for specific runs (live-progress poll). Column order must match
    // the RunProgressResponse record (Dapper binds positionally). ExecutedRequest + ProgressPercent
    // are tracked in memory (selected as NULL); the query handler fills them for running runs.
    public const string GetProgressByIds =
        "SELECT Id, Status, TotalRequest, CAST(NULL AS INT) AS ExecutedRequest, "
        + "PassCount, WarningCount, FailedCount, DurationMs, CAST(NULL AS INT) AS ProgressPercent "
        + "FROM Run WHERE ProjectId = @ProjectId AND Id IN @Ids;";

    public const string GetEntityById =
        "SELECT Id, ProjectId, DatasetId, Title, Description, Environment, RunType, Status, RequestLimitPerApi, "
        + "MaxParallelApis, MaxParallelRequestsPerApi, TotalRequest, "
        + "PassCount, WarningCount, FailedCount, DurationMs, StartTime, EndTime, CreatedDate FROM Run WHERE Id = @Id;";

    /// <summary>Target API ids for a run (from RunApi). Empty = the run targets all of the dataset's APIs.</summary>
    public const string GetApiIds = "SELECT ApiId FROM RunApi WHERE RunId = @RunId ORDER BY ApiId;";

    /// <summary>Updates an editable (Pending) run's fields. Affects 0 rows if the run isn't Pending.</summary>
    public const string Update =
        "UPDATE Run SET DatasetId = @DatasetId, Title = @Title, Description = @Description, Environment = @Environment, "
        + "RunType = @RunType, RequestLimitPerApi = @RequestLimitPerApi, MaxParallelApis = @MaxParallelApis, "
        + "MaxParallelRequestsPerApi = @MaxParallelRequestsPerApi WHERE Id = @Id AND Status = 'Pending';";

    public const string DeleteApis = "DELETE FROM RunApi WHERE RunId = @RunId;";

    /// <summary>
    /// Per-API execution stats. Pass = OK (HTTP &lt; 400) and &lt;= 2000ms; Warning = OK but &gt; 2000ms;
    /// Failed = error or HTTP &gt;= 400.
    /// </summary>
    public const string ApiStatistics =
        "SELECT a.Id AS ApiId, a.Name AS ApiName, a.Method, a.Endpoint, COUNT(1) AS RequestCount, "
        + "SUM(CASE WHEN rr.ErrorMessage IS NULL AND rr.StatusCode >= 200 AND rr.StatusCode < 400 AND ISNULL(rr.DurationMs, 0) <= 2000 THEN 1 ELSE 0 END) AS PassedCount, "
        + "SUM(CASE WHEN rr.ErrorMessage IS NULL AND rr.StatusCode >= 200 AND rr.StatusCode < 400 AND rr.DurationMs > 2000 THEN 1 ELSE 0 END) AS WarningCount, "
        + "SUM(CASE WHEN rr.ErrorMessage IS NOT NULL OR rr.StatusCode IS NULL OR rr.StatusCode >= 400 THEN 1 ELSE 0 END) AS FailedCount, "
        + "AVG(CAST(rr.DurationMs AS FLOAT)) AS AvgResponseTimeMs "
        + "FROM RunResponse rr "
        + "INNER JOIN ApiRequest req ON req.Id = rr.RequestId "
        + "INNER JOIN Api a ON a.Id = req.ApiId "
        + "WHERE rr.RunId = @RunId "
        + "GROUP BY a.Id, a.Name, a.Method, a.Endpoint ORDER BY a.Name;";

    /// <summary>
    /// Pending → Queued only (guards double-start). Affects 0 rows if not Pending. The background
    /// worker moves Queued → Running when execution actually begins (see RunExecutionSql.MarkRunning).
    /// </summary>
    public const string MarkQueued =
        "UPDATE Run SET Status = 'Queued' WHERE Id = @Id AND Status = 'Pending';";

    public const string Complete =
        "UPDATE Run SET Status = @Status, EndTime = @EndTime, DurationMs = @DurationMs, TotalRequest = @TotalRequest, "
        + "PassCount = @PassCount, WarningCount = @WarningCount, FailedCount = @FailedCount WHERE Id = @Id;";

    // Per-request status, derived from the captured response (same rule as the executor).
    private const string ResponseStatusCase =
        "CASE WHEN rr.ErrorMessage IS NOT NULL OR rr.StatusCode IS NULL OR rr.StatusCode >= 400 THEN 'Failed' "
        + "WHEN rr.DurationMs > 2000 THEN 'Warning' ELSE 'Passed' END";

    private const string ResponsesFrom =
        "FROM RunResponse rr INNER JOIN ApiRequest req ON req.Id = rr.RequestId INNER JOIN Api a ON a.Id = req.ApiId "
        + "WHERE rr.RunId = @RunId "
        + "AND (@Search IS NULL OR req.Title LIKE @Search ESCAPE '\\' OR a.Endpoint LIKE @Search ESCAPE '\\') "
        + $"AND (@Status IS NULL OR ({ResponseStatusCase}) = @Status) "
        + "AND (@ApiId IS NULL OR req.ApiId = @ApiId)";

    public const string ResponsesCount = $"SELECT COUNT(1) {ResponsesFrom};";

    /// <summary>
    /// Paged response list. <paramref name="orderByColumn"/> and <paramref name="direction"/> MUST come
    /// from a validated whitelist (never user input) — they are interpolated, not parameterized.
    /// rr.RequestId is appended as a stable tiebreaker so paging is deterministic.
    /// </summary>
    public static string ResponsesPaged(string orderByColumn, string direction)
    {
        // rr.RequestId is the stable tiebreaker; skip it when it's already the primary key
        // (ORDER BY must not list the same column twice).
        var tiebreaker = orderByColumn == "rr.RequestId" ? string.Empty : ", rr.RequestId ASC";
        return "SELECT rr.RequestId, req.Title, a.Method, a.Endpoint, rr.StatusCode, rr.DurationMs, rr.ResponseSize, "
            + $"{ResponseStatusCase} AS Status, rr.ErrorMessage, rr.CreatedDate "
            + ResponsesFrom
            + $" ORDER BY {orderByColumn} {direction}{tiebreaker} OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";
    }

    /// <summary>
    /// Every executed request of a run with its input + full response body, for the Excel export
    /// (column order matches RunResponseExportRow). Ordered by API then title for a grouped read.
    /// </summary>
    public const string ResponseExport =
        "SELECT rr.RequestId, a.Name AS ApiName, a.Method, a.Endpoint, req.Title, "
        + "req.QueryStringJson AS RequestQueryJson, req.RequestBody, rr.ResponseJson, "
        + "rr.StatusCode, rr.DurationMs, rr.ErrorMessage "
        + "FROM RunResponse rr INNER JOIN ApiRequest req ON req.Id = rr.RequestId INNER JOIN Api a ON a.Id = req.ApiId "
        + "WHERE rr.RunId = @RunId "
        + "ORDER BY a.Name, req.Title, rr.RequestId;";

    // Single executed request detail (Results tab). Column order must match RunResponseDetail (Dapper positional).
    public const string ResponseDetail =
        "SELECT rr.RequestId, req.Title, a.Method, a.Endpoint, rr.StatusCode, rr.DurationMs, rr.ResponseSize, "
        + $"{ResponseStatusCase} AS Status, rr.ErrorMessage, rr.CreatedDate, "
        + "rr.ResponseJson, rr.ResponseHeaders AS ResponseHeadersJson, "
        + "req.HeadersJson AS RequestHeadersJson, req.QueryStringJson AS RequestQueryJson, req.RequestBody "
        + "FROM RunResponse rr INNER JOIN ApiRequest req ON req.Id = rr.RequestId INNER JOIN Api a ON a.Id = req.ApiId "
        + "WHERE rr.RunId = @RunId AND rr.RequestId = @RequestId;";

    /// <summary>
    /// Queued/Running → Cancelled only (sets EndTime). Affects 0 rows otherwise. A cancelled Queued
    /// run is skipped by the worker when dequeued (its Queued → Running update matches 0 rows).
    /// </summary>
    public const string MarkCancelled =
        "UPDATE Run SET Status = 'Cancelled', EndTime = @EndTime, "
        + "DurationMs = CASE WHEN StartTime IS NOT NULL THEN DATEDIFF(MILLISECOND, StartTime, @EndTime) ELSE DurationMs END "
        + "WHERE Id = @Id AND Status IN ('Queued', 'Running');";

    /// <summary>1 if this run is used (as baseline/current) in any compare job or result; else 0.</summary>
    public const string HasCompare =
        "SELECT CASE WHEN EXISTS (SELECT 1 FROM CompareJob WHERE RunAId = @Id OR RunBId = @Id) "
        + "             OR EXISTS (SELECT 1 FROM CompareResult WHERE RunAId = @Id OR RunBId = @Id) "
        + "          THEN 1 ELSE 0 END;";

    /// <summary>
    /// Deletes a run and its captured responses (RunApi is removed by ON DELETE CASCADE). Only ever
    /// called for runs with NO compare (the handler blocks otherwise), so no compare/report cleanup.
    /// </summary>
    public const string Delete =
        "DELETE FROM RunResponse WHERE RunId = @Id;\n" + "DELETE FROM Run WHERE Id = @Id;";

    public const string Insert =
        "INSERT INTO Run (ProjectId, DatasetId, Title, Description, Environment, RunType, Status, RequestLimitPerApi, "
        + "MaxParallelApis, MaxParallelRequestsPerApi, TotalRequest, CreatedDate) "
        + "OUTPUT INSERTED.Id "
        + "VALUES (@ProjectId, @DatasetId, @Title, @Description, @Environment, @RunType, @Status, @RequestLimitPerApi, "
        + "@MaxParallelApis, @MaxParallelRequestsPerApi, @TotalRequest, SYSUTCDATETIME());";

    public const string InsertApi = "INSERT INTO RunApi (RunId, ApiId) VALUES (@RunId, @ApiId);";
}
