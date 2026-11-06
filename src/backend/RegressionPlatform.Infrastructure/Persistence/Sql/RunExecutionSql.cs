namespace RegressionPlatform.Infrastructure.Persistence.Sql;

/// <summary>Parameterized SQL used by the run executor (background worker).</summary>
internal static class RunExecutionSql
{
    public const string GetExecutionInfo =
        "SELECT Id, ProjectId, DatasetId, Environment, RequestLimitPerApi, "
        + "MaxParallelApis, MaxParallelRequestsPerApi FROM Run WHERE Id = @Id;";

    public const string GetTargetApiIds = "SELECT ApiId FROM RunApi WHERE RunId = @RunId;";

    private const string RunnableSelect =
        "SELECT r.Id AS RequestId, a.Id AS ApiId, a.Method, a.Endpoint, r.HeadersJson, r.QueryStringJson, r.RequestBody "
        + "FROM ApiRequest r INNER JOIN Api a ON a.Id = r.ApiId "
        + "WHERE r.DatasetId = @DatasetId AND r.IsActive = 1 AND r.IsDeleted = 0 AND a.IsActive = 1";

    /// <summary>All active requests in the dataset.</summary>
    public const string RunnableAll = RunnableSelect + " ORDER BY r.Id;";

    /// <summary>Active requests for the given API ids (use with an @ApiIds list parameter).</summary>
    public const string RunnableForApis = RunnableSelect + " AND r.ApiId IN @ApiIds ORDER BY r.Id;";

    /// <summary>Enabled ignore rules + their API links (ApiId null = applies to all APIs).</summary>
    public const string IgnorePaths =
        "SELECT cir.Id AS RuleId, cir.JsonPath, cira.ApiId "
        + "FROM CompareIgnoreRule cir "
        + "LEFT JOIN CompareIgnoreRuleApi cira ON cira.IgnoreRuleId = cir.Id "
        + "WHERE cir.ProjectId = @ProjectId AND cir.Enabled = 1;";

    public const string InsertResponse =
        "INSERT INTO RunResponse (RunId, RequestId, StatusCode, DurationMs, ResponseHash, ResponseOrderedHash, ResponseJson, ResponseHeaders, ResponseSize, ErrorMessage, CreatedDate) "
        + "VALUES (@RunId, @RequestId, @StatusCode, @DurationMs, @ResponseHash, @ResponseOrderedHash, @ResponseJson, @ResponseHeaders, @ResponseSize, @ErrorMessage, SYSUTCDATETIME());";

    /// <summary>One request's definition by id (for a single-request re-run). No IsActive filter (the user
    /// re-runs this exact request), but a soft-deleted request is never runnable.</summary>
    public const string RunnableById =
        "SELECT r.Id AS RequestId, a.Id AS ApiId, a.Method, a.Endpoint, r.HeadersJson, r.QueryStringJson, r.RequestBody "
        + "FROM ApiRequest r INNER JOIN Api a ON a.Id = r.ApiId WHERE r.Id = @RequestId AND r.IsDeleted = 0;";

    /// <summary>Removes a request's existing response in a run (before inserting the re-run result).</summary>
    public const string DeleteResponse = "DELETE FROM RunResponse WHERE RunId = @RunId AND RequestId = @RequestId;";

    /// <summary>
    /// Recomputes a run's TotalRequest + health counts from its responses (same rule as ApiStatistics:
    /// Pass = OK &amp; ≤ 2000ms, Warning = OK &amp; &gt; 2000ms, Failed = error/no-status/HTTP ≥ 400).
    /// </summary>
    public const string RecalculateCounts =
        "UPDATE Run SET "
        + "TotalRequest = (SELECT COUNT(1) FROM RunResponse WHERE RunId = @Id), "
        + "PassCount = (SELECT COUNT(1) FROM RunResponse WHERE RunId = @Id AND ErrorMessage IS NULL AND StatusCode >= 200 AND StatusCode < 400 AND ISNULL(DurationMs, 0) <= 2000), "
        + "WarningCount = (SELECT COUNT(1) FROM RunResponse WHERE RunId = @Id AND ErrorMessage IS NULL AND StatusCode >= 200 AND StatusCode < 400 AND DurationMs > 2000), "
        + "FailedCount = (SELECT COUNT(1) FROM RunResponse WHERE RunId = @Id AND (ErrorMessage IS NOT NULL OR StatusCode IS NULL OR StatusCode >= 400)) "
        + "WHERE Id = @Id;";

    public const string Complete =
        "UPDATE Run SET Status = @Status, EndTime = @EndTime, DurationMs = @DurationMs, TotalRequest = @TotalRequest, "
        + "PassCount = @PassCount, WarningCount = @WarningCount, FailedCount = @FailedCount WHERE Id = @Id;";

    /// <summary>
    /// Queued → Running only (sets StartTime), right before execution begins. Affects 0 rows if the
    /// run was cancelled (or otherwise finalized) while waiting in the queue — the worker then skips it.
    /// </summary>
    public const string MarkRunning =
        "UPDATE Run SET Status = 'Running', StartTime = @StartTime WHERE Id = @Id AND Status = 'Queued';";

    /// <summary>
    /// Startup recovery: any run still 'Running' was interrupted by an app restart (the in-memory
    /// queue/executor did not survive) — finalize it as Failed. DurationMs is left as-is: the run
    /// never finished, so a computed duration would be meaningless.
    /// </summary>
    public const string FailInterrupted =
        "UPDATE Run SET Status = 'Failed', EndTime = @EndTime WHERE Status = 'Running';";

    /// <summary>Startup recovery: Queued runs never started, so they are safe to re-enqueue.</summary>
    public const string GetQueuedRunIds = "SELECT Id FROM Run WHERE Status = 'Queued' ORDER BY Id;";
}
