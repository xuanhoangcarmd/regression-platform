namespace RegressionPlatform.Infrastructure.Persistence.Sql;

/// <summary>Parameterized SQL for compares (CompareJob, CompareJobApi, CompareResult, CompareDetail).</summary>
internal static class CompareSql
{
    // ----- Create -----

    public const string InsertJob =
        "INSERT INTO CompareJob (ProjectId, RunAId, RunBId, Status, Progress, BaselineMode, IgnoreOrder, CreatedDate) "
        + "OUTPUT INSERTED.Id "
        + "VALUES (@ProjectId, @RunAId, @RunBId, @Status, 0, @BaselineMode, @IgnoreOrder, SYSUTCDATETIME());";

    public const string InsertJobApi = "INSERT INTO CompareJobApi (CompareJobId, ApiId) VALUES (@CompareJobId, @ApiId);";

    // ----- Query: History list (column order matches CompareJobListItem) -----

    public const string GetJobsByProject =
        "SELECT j.Id, j.RunAId, j.RunBId, ra.Title AS RunATitle, rb.Title AS RunBTitle, j.Status, j.Progress, "
        + "j.CompareResultId, r.TotalRequest, r.UnchangedCount, r.ChangedCount, r.AddedCount, r.RemovedCount, r.FailedCount, "
        + "j.FinishedDate, j.CreatedDate, "
        // HasReport: a report was generated from this compare → not deletable.
        + "CAST(CASE WHEN EXISTS (SELECT 1 FROM Report rp WHERE rp.CompareJobId = j.Id) THEN 1 ELSE 0 END AS BIT) AS HasReport "
        + "FROM CompareJob j "
        + "INNER JOIN Run ra ON ra.Id = j.RunAId "
        + "INNER JOIN Run rb ON rb.Id = j.RunBId "
        + "LEFT JOIN CompareResult r ON r.Id = j.CompareResultId "
        + "WHERE j.ProjectId = @ProjectId "
        + "ORDER BY j.CreatedDate DESC, j.Id DESC;";

    // ----- Query: job entity (ownership/status checks) -----

    public const string GetJobEntity =
        "SELECT Id, ProjectId, RunAId, RunBId, Status, Progress, CompareResultId, BaselineMode, IgnoreOrder, StartedDate, FinishedDate, CreatedDate "
        + "FROM CompareJob WHERE Id = @Id;";

    // ----- Delete -----

    /// <summary>1 if a report was generated from this compare; else 0.</summary>
    public const string HasReport =
        "SELECT CASE WHEN EXISTS (SELECT 1 FROM Report WHERE CompareJobId = @Id) THEN 1 ELSE 0 END;";

    /// <summary>
    /// Deletes a compare and everything derived from it. Order is FK-safe: the job (which references
    /// its CompareResult) goes first, then the result's details, then the result. CompareJobApi is
    /// removed by ON DELETE CASCADE. Run within a transaction by the repository.
    /// </summary>
    public const string Delete =
        "DECLARE @ResultId BIGINT = (SELECT CompareResultId FROM CompareJob WHERE Id = @Id);\n"
        + "DELETE FROM CompareJobApi WHERE CompareJobId = @Id;\n"
        + "DELETE FROM CompareJob WHERE Id = @Id;\n"
        + "IF @ResultId IS NOT NULL\n"
        + "BEGIN\n"
        + "    DELETE FROM CompareDetail WHERE CompareResultId = @ResultId;\n"
        + "    DELETE FROM CompareResult WHERE Id = @ResultId;\n"
        + "END;";

    // ----- Query: detail header (column order matches CompareSummaryResponse) -----

    public const string GetSummary =
        "SELECT j.Id, j.ProjectId, j.RunAId, j.RunBId, ra.Title AS RunATitle, rb.Title AS RunBTitle, j.Status, j.Progress, "
        + "j.CompareResultId, r.TotalRequest, r.UnchangedCount, r.ChangedCount, r.AddedCount, r.RemovedCount, r.FailedCount, "
        + "j.StartedDate, j.FinishedDate, j.CreatedDate, j.BaselineMode, j.IgnoreOrder "
        + "FROM CompareJob j "
        + "INNER JOIN Run ra ON ra.Id = j.RunAId "
        + "INNER JOIN Run rb ON rb.Id = j.RunBId "
        + "LEFT JOIN CompareResult r ON r.Id = j.CompareResultId "
        + "WHERE j.Id = @Id;";

    // ----- Query: change-by-API breakdown -----

    public const string ApiStatistics =
        "SELECT a.Id AS ApiId, a.Name AS ApiName, a.Method, a.Endpoint, COUNT(1) AS RequestCount, "
        + "SUM(CASE WHEN cd.Result = 'Unchanged' THEN 1 ELSE 0 END) AS UnchangedCount, "
        + "SUM(CASE WHEN cd.Result = 'Changed' THEN 1 ELSE 0 END) AS ChangedCount, "
        + "SUM(CASE WHEN cd.Result = 'Added' THEN 1 ELSE 0 END) AS AddedCount, "
        + "SUM(CASE WHEN cd.Result = 'Removed' THEN 1 ELSE 0 END) AS RemovedCount, "
        + "SUM(CASE WHEN cd.Result = 'Failed' THEN 1 ELSE 0 END) AS FailedCount "
        + "FROM CompareDetail cd "
        + "INNER JOIN ApiRequest req ON req.Id = cd.RequestId "
        + "INNER JOIN Api a ON a.Id = req.ApiId "
        + "WHERE cd.CompareResultId = @CompareResultId "
        + "GROUP BY a.Id, a.Name, a.Method, a.Endpoint ORDER BY a.Name;";

    // ----- Query: per-request detail list (paged + filter + sort) -----

    private const string DetailsFrom =
        "FROM CompareDetail cd INNER JOIN ApiRequest req ON req.Id = cd.RequestId INNER JOIN Api a ON a.Id = req.ApiId "
        + "WHERE cd.CompareResultId = @CompareResultId "
        + "AND (@Search IS NULL OR req.Title LIKE @Search ESCAPE '\\' OR a.Endpoint LIKE @Search ESCAPE '\\') "
        + "AND (@Result IS NULL OR cd.Result = @Result) "
        + "AND (@ApiId IS NULL OR req.ApiId = @ApiId) "
        // Change-pattern filter: the request's field-diff contains the chosen (path, changeType) pair.
        + "AND (@DiffLike IS NULL OR cd.DiffJson LIKE @DiffLike ESCAPE '\\')";

    public const string DetailsCount = $"SELECT COUNT(1) {DetailsFrom};";

    /// <summary>
    /// Paged detail list. <paramref name="orderByColumn"/> and <paramref name="direction"/> MUST come
    /// from a validated whitelist (never user input). cd.RequestId is a stable paging tiebreaker.
    /// </summary>
    public static string DetailsPaged(string orderByColumn, string direction)
    {
        var tiebreaker = orderByColumn == "cd.RequestId" ? string.Empty : ", cd.RequestId ASC";
        return "SELECT cd.RequestId, req.Title, a.Method, a.Endpoint, cd.Result, cd.IsEdited "
            + DetailsFrom
            + $" ORDER BY {orderByColumn} {direction}{tiebreaker} OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";
    }

    // ----- Query: all per-request details for a result (full export) -----

    /// <summary>Every request's identity + result + raw diff JSON (column order matches CompareFullDetailItem).</summary>
    public const string FullDetails =
        "SELECT cd.RequestId, req.Title, a.Method, a.Endpoint, cd.Result, cd.DiffJson, req.QueryStringJson, req.RequestBody "
        + "FROM CompareDetail cd "
        + "INNER JOIN ApiRequest req ON req.Id = cd.RequestId "
        + "INNER JOIN Api a ON a.Id = req.ApiId "
        + "WHERE cd.CompareResultId = @CompareResultId ORDER BY a.Name, req.Title, cd.RequestId;";

    // ----- Each run's edited (expected) response, for requests compared with an edit (cd.IsEdited = 1) -----
    // The joins only touch RunResponseOverride for edited rows (unique (RunId, RequestId) seek);
    // otherwise every column is the captured RunResponse, exactly as before.

    private const string ExpectedJoins =
        "LEFT JOIN RunResponseOverride oa ON cd.IsEdited = 1 AND oa.RunId = @RunAId AND oa.RequestId = cd.RequestId "
        + "LEFT JOIN RunResponseOverride ob ON cd.IsEdited = 1 AND ob.RunId = @RunBId AND ob.RequestId = cd.RequestId ";

    private const string StatusColumns =
        "CASE WHEN oa.Id IS NOT NULL AND oa.StatusCode IS NOT NULL THEN oa.StatusCode ELSE ra.StatusCode END AS StatusCodeA, "
        + "CASE WHEN ob.Id IS NOT NULL AND ob.StatusCode IS NOT NULL THEN ob.StatusCode ELSE rb.StatusCode END AS StatusCodeB";

    private const string ErrorColumns =
        "CASE WHEN oa.Id IS NULL THEN ra.ErrorMessage END AS ErrorMessageA, "
        + "CASE WHEN ob.Id IS NULL THEN rb.ErrorMessage END AS ErrorMessageB";

    private const string BodyColumns =
        "COALESCE(oa.ResponseJson, ra.ResponseJson) AS ResponseAJson, COALESCE(ob.ResponseJson, rb.ResponseJson) AS ResponseBJson";

    // ----- Query: every request with BOTH runs' stored responses (column order matches CompareTestResultItem) -----

    public const string TestResults =
        "SELECT cd.RequestId, a.Name AS ApiName, a.Method, a.Endpoint, req.Title, req.Description, "
        + "req.QueryStringJson, req.RequestBody, cd.Result, cd.DiffJson, "
        + StatusColumns + ", "
        + "ra.DurationMs AS DurationMsA, rb.DurationMs AS DurationMsB, "
        + ErrorColumns + ", "
        + BodyColumns + ", cd.IsEdited "
        + "FROM CompareDetail cd "
        + "INNER JOIN ApiRequest req ON req.Id = cd.RequestId "
        + "INNER JOIN Api a ON a.Id = req.ApiId "
        + "LEFT JOIN RunResponse ra ON ra.RunId = @RunAId AND ra.RequestId = cd.RequestId "
        + "LEFT JOIN RunResponse rb ON rb.RunId = @RunBId AND rb.RequestId = cd.RequestId "
        + ExpectedJoins
        + "WHERE cd.CompareResultId = @CompareResultId ORDER BY req.Title, a.Name, cd.RequestId;";

    // ----- Query: all per-request diffs for a result (change summary) -----

    public const string GetDiffs =
        "SELECT cd.RequestId, req.ApiId, a.Name AS ApiName, a.Method, a.Endpoint, cd.DiffJson "
        + "FROM CompareDetail cd "
        + "INNER JOIN ApiRequest req ON req.Id = cd.RequestId "
        + "INNER JOIN Api a ON a.Id = req.ApiId "
        + "WHERE cd.CompareResultId = @CompareResultId AND cd.DiffJson IS NOT NULL;";

    // ----- Query: single request diff + both runs' bodies (column order matches CompareDetailDiffResponse) -----

    public const string DetailDiff =
        "SELECT cd.RequestId, req.Title, a.Method, a.Endpoint, cd.Result, cd.DiffJson, "
        + StatusColumns + ", "
        + BodyColumns + ", "
        + ErrorColumns + ", req.ApiId, "
        + "CAST(CASE WHEN oa.Id IS NULL THEN 0 ELSE 1 END AS BIT) AS IsEditedA, "
        + "CAST(CASE WHEN ob.Id IS NULL THEN 0 ELSE 1 END AS BIT) AS IsEditedB "
        + "FROM CompareDetail cd "
        + "INNER JOIN ApiRequest req ON req.Id = cd.RequestId "
        + "INNER JOIN Api a ON a.Id = req.ApiId "
        + "LEFT JOIN RunResponse ra ON ra.RunId = @RunAId AND ra.RequestId = cd.RequestId "
        + "LEFT JOIN RunResponse rb ON rb.RunId = @RunBId AND rb.RequestId = cd.RequestId "
        + ExpectedJoins
        + "WHERE cd.CompareResultId = @CompareResultId AND cd.RequestId = @RequestId;";

    // ----- Execution (worker) -----

    public const string GetExecutionInfo = "SELECT Id, ProjectId, RunAId, RunBId, BaselineMode, IgnoreOrder FROM CompareJob WHERE Id = @Id;";

    public const string GetScopeApiIds = "SELECT ApiId FROM CompareJobApi WHERE CompareJobId = @JobId;";

    // Covered by UX_Run_Request INCLUDE (ResponseHash, StatusCode, ErrorMessage) → no key lookups.
    private const string ResponseSummarySelect =
        "SELECT rr.RequestId, req.ApiId, rr.ResponseHash, COALESCE(rr.ResponseOrderedHash, rr.ResponseHash) AS OrderedHash, "
        + "rr.StatusCode, rr.ErrorMessage, CAST(0 AS BIT) AS IsEdited "
        + "FROM RunResponse rr INNER JOIN ApiRequest req ON req.Id = rr.RequestId ";

    public const string ResponseSummariesAll = ResponseSummarySelect + "WHERE rr.RunId = @RunId;";

    public const string ResponseSummariesForApis =
        ResponseSummarySelect + "WHERE rr.RunId = @RunId AND req.ApiId IN @ApiIds;";

    /// <summary>
    /// A run in 'Expected' mode: the edited response's hash / status (and no error) when one exists,
    /// else the captured one. Same columns as <see cref="ResponseSummarySelect"/>.
    /// </summary>
    private const string ExpectedSummarySelect =
        "SELECT rr.RequestId, req.ApiId, COALESCE(o.ResponseHash, rr.ResponseHash) AS ResponseHash, "
        + "CASE WHEN o.Id IS NOT NULL THEN COALESCE(o.ResponseOrderedHash, o.ResponseHash) "
        + "ELSE COALESCE(rr.ResponseOrderedHash, rr.ResponseHash) END AS OrderedHash, "
        + "CASE WHEN o.Id IS NOT NULL AND o.StatusCode IS NOT NULL THEN o.StatusCode ELSE rr.StatusCode END AS StatusCode, "
        + "CASE WHEN o.Id IS NULL THEN rr.ErrorMessage END AS ErrorMessage, "
        + "CAST(CASE WHEN o.Id IS NULL THEN 0 ELSE 1 END AS BIT) AS IsEdited "
        + "FROM RunResponse rr INNER JOIN ApiRequest req ON req.Id = rr.RequestId "
        + "LEFT JOIN RunResponseOverride o ON o.RunId = rr.RunId AND o.RequestId = rr.RequestId ";

    public const string ExpectedSummariesAll = ExpectedSummarySelect + "WHERE rr.RunId = @RunId;";

    public const string ExpectedSummariesForApis =
        ExpectedSummarySelect + "WHERE rr.RunId = @RunId AND req.ApiId IN @ApiIds;";

    /// <summary>Enabled compare ignore rules + their API links (ApiId null = applies to all APIs).</summary>
    public const string IgnorePaths =
        "SELECT cir.Id AS RuleId, cir.JsonPath, cira.ApiId "
        + "FROM CompareIgnoreRule cir "
        + "LEFT JOIN CompareIgnoreRuleApi cira ON cira.IgnoreRuleId = cir.Id "
        + "WHERE cir.ProjectId = @ProjectId AND cir.Enabled = 1 AND cir.RuleType = 'Ignore';";

    /// <summary>
    /// Enabled extract-root rules + their API links (ApiId null = applies to all APIs). A rule lifts
    /// the subtree at its JsonPath to the response root before comparison (envelope unwrapping).
    /// </summary>
    public const string ExtractRootPaths =
        "SELECT cir.Id AS RuleId, cir.JsonPath, cira.ApiId "
        + "FROM CompareIgnoreRule cir "
        + "LEFT JOIN CompareIgnoreRuleApi cira ON cira.IgnoreRuleId = cir.Id "
        + "WHERE cir.ProjectId = @ProjectId AND cir.Enabled = 1 AND cir.RuleType = 'ExtractRoot';";

    /// <summary>Enabled TrimAfter rules + their API links (ApiId null = applies to all APIs).</summary>
    public const string TrimRules =
        "SELECT cir.Id AS RuleId, cir.JsonPath, cir.Delimiter, cira.ApiId "
        + "FROM CompareIgnoreRule cir "
        + "LEFT JOIN CompareIgnoreRuleApi cira ON cira.IgnoreRuleId = cir.Id "
        + "WHERE cir.ProjectId = @ProjectId AND cir.Enabled = 1 AND cir.RuleType = 'TrimAfter' "
        + "AND cir.Delimiter IS NOT NULL AND cir.Delimiter <> '';";

    /// <summary>Both runs' bodies for a batch of deep-compare requests (≤ 500 ids — Dapper expands IN).</summary>
    public const string GetResponseBodies =
        "SELECT ra.RequestId, ra.ResponseJson AS JsonA, rb.ResponseJson AS JsonB "
        + "FROM RunResponse ra "
        + "INNER JOIN RunResponse rb ON rb.RunId = @RunBId AND rb.RequestId = ra.RequestId "
        + "WHERE ra.RunId = @RunAId AND ra.RequestId IN @RequestIds;";

    /// <summary>As <see cref="GetResponseBodies"/>, with each run's edited (expected) body when one exists.</summary>
    public const string GetExpectedResponseBodies =
        "SELECT ra.RequestId, COALESCE(oa.ResponseJson, ra.ResponseJson) AS JsonA, COALESCE(ob.ResponseJson, rb.ResponseJson) AS JsonB "
        + "FROM RunResponse ra "
        + "INNER JOIN RunResponse rb ON rb.RunId = @RunBId AND rb.RequestId = ra.RequestId "
        + "LEFT JOIN RunResponseOverride oa ON oa.RunId = ra.RunId AND oa.RequestId = ra.RequestId "
        + "LEFT JOIN RunResponseOverride ob ON ob.RunId = rb.RunId AND ob.RequestId = rb.RequestId "
        + "WHERE ra.RunId = @RunAId AND ra.RequestId IN @RequestIds;";

    /// <summary>
    /// Marks a job Running only while it is still 'Running' (the state Start set before enqueue). If a
    /// Stop flipped it to 'Cancelled' while it waited in the queue, this affects 0 rows so the worker
    /// skips it instead of resurrecting it.
    /// </summary>
    public const string MarkRunning =
        "UPDATE CompareJob SET Status = 'Running', StartedDate = @StartedDate, Progress = 0 WHERE Id = @Id AND Status = 'Running';";

    /// <summary>Running → Cancelled (guards Stop). Affects 0 rows if the job isn't Running.</summary>
    public const string MarkCancelled =
        "UPDATE CompareJob SET Status = 'Cancelled', FinishedDate = @FinishedDate WHERE Id = @Id AND Status = 'Running';";

    /// <summary>Pending → Running only (guards double-start). Affects 0 rows if not Pending.</summary>
    public const string TryStart =
        "UPDATE CompareJob SET Status = 'Running', StartedDate = @StartedDate, Progress = 0 WHERE Id = @Id AND Status = 'Pending';";

    public const string UpdateProgress = "UPDATE CompareJob SET Progress = @Progress WHERE Id = @Id;";

    public const string InsertResult =
        "INSERT INTO CompareResult (RunAId, RunBId, TotalRequest, UnchangedCount, ChangedCount, FailedCount, AddedCount, RemovedCount, ModifiedCount, CreatedDate) "
        + "OUTPUT INSERTED.Id "
        + "VALUES (@RunAId, @RunBId, @TotalRequest, @UnchangedCount, @ChangedCount, @FailedCount, @AddedCount, @RemovedCount, @ModifiedCount, SYSUTCDATETIME());";

    /// <summary>Target of the SqlBulkCopy that writes a compare's per-request details.</summary>
    public const string DetailTable = "dbo.CompareDetail";

    public const string CompleteJob =
        "UPDATE CompareJob SET Status = 'Completed', CompareResultId = @CompareResultId, FinishedDate = @FinishedDate, Progress = 100 WHERE Id = @Id;";

    public const string MarkJobFailed = "UPDATE CompareJob SET Status = 'Failed', FinishedDate = @FinishedDate WHERE Id = @Id;";
}
