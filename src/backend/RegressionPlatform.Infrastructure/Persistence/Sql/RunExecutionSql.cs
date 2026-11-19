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

    /// <summary>
    /// Finalizes a run — only while it is still Running AND owned by this instance, so an executor
    /// that lost the run (stopped, re-run, or failed by recovery) never overwrites its new state.
    /// </summary>
    public const string Complete =
        "UPDATE Run SET Status = @Status, EndTime = @EndTime, DurationMs = @DurationMs, TotalRequest = @TotalRequest, "
        + "PassCount = @PassCount, WarningCount = @WarningCount, FailedCount = @FailedCount "
        + "WHERE Id = @Id AND Status = 'Running' AND ExecutorInstance = @Instance;";

    /// <summary>
    /// Queued → Running only (sets StartTime), right before execution begins, and claims the run for
    /// this instance (owner + first heartbeat). Affects 0 rows if the run was cancelled (or otherwise
    /// finalized / claimed) while waiting in the queue — the worker then skips it.
    /// </summary>
    public const string MarkRunning =
        "UPDATE Run SET Status = 'Running', StartTime = @StartTime, ExecutorInstance = @Instance, HeartbeatAt = SYSUTCDATETIME() "
        + "WHERE Id = @Id AND Status = 'Queued';";

    /// <summary>
    /// Owner heartbeat while executing. 0 rows = the run is no longer Running under this instance
    /// (stopped from any instance, re-run, or failed by recovery) — the executor must stop.
    /// </summary>
    public const string Heartbeat =
        "UPDATE Run SET HeartbeatAt = SYSUTCDATETIME() WHERE Id = @Id AND Status = 'Running' AND ExecutorInstance = @Instance;";

    /// <summary>
    /// Inserts a response only while this instance still owns the Running run, and never twice for
    /// the same request. Returns -1 = not the owner any more, 0 = already stored (skipped), 1 = inserted.
    /// </summary>
    public const string InsertOwnedResponse =
        "IF NOT EXISTS (SELECT 1 FROM Run WHERE Id = @RunId AND Status = 'Running' AND ExecutorInstance = @Instance) "
        + "    SELECT -1; "
        + "ELSE "
        + "BEGIN "
        + "    INSERT INTO RunResponse (RunId, RequestId, StatusCode, DurationMs, ResponseHash, ResponseOrderedHash, ResponseJson, ResponseHeaders, ResponseSize, ErrorMessage, CreatedDate) "
        + "    SELECT @RunId, @RequestId, @StatusCode, @DurationMs, @ResponseHash, @ResponseOrderedHash, @ResponseJson, @ResponseHeaders, @ResponseSize, @ErrorMessage, SYSUTCDATETIME() "
        + "    WHERE NOT EXISTS (SELECT 1 FROM RunResponse WITH (UPDLOCK, HOLDLOCK) WHERE RunId = @RunId AND RequestId = @RequestId); "
        + "    SELECT @@ROWCOUNT; "
        + "END";

    /// <summary>
    /// Recovery: a Running run whose owner stopped heart-beating (process killed / restarted / stuck)
    /// is finalized as Failed. Runs owned by a live instance — on this or another machine sharing the
    /// DB — keep beating and are left alone. Legacy rows (no owner, from before ownership existed) are
    /// never auto-failed; stop them manually. DurationMs is left as-is (the run never finished).
    /// </summary>
    public const string FailStale =
        "UPDATE Run SET Status = 'Failed', EndTime = @EndTime "
        + "WHERE Status = 'Running' AND ExecutorInstance IS NOT NULL "
        + "  AND (HeartbeatAt IS NULL OR HeartbeatAt < DATEADD(SECOND, -@StaleSeconds, SYSUTCDATETIME()));";

    /// <summary>
    /// Startup recovery: Queued runs never started, so they are safe to re-enqueue — but only the ones
    /// queued from this node (or legacy rows with no owner), so a restart of one machine doesn't pull
    /// runs that another instance sharing the DB queued.
    /// </summary>
    public const string GetQueuedRunIds =
        "SELECT Id FROM Run WHERE Status = 'Queued' "
        + "AND (ExecutorInstance IS NULL OR LEFT(ExecutorInstance, LEN(@NodePrefix)) = @NodePrefix) ORDER BY Id;";
}
