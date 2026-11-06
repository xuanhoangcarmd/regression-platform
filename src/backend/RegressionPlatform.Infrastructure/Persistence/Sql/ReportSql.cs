namespace RegressionPlatform.Infrastructure.Persistence.Sql;

/// <summary>Parameterized SQL for reports (the <c>Report</c> table; 1 report per compare).</summary>
internal static class ReportSql
{
    // ----- Upsert (one report per compare — keyed by CompareJobId) -----

    /// <summary>
    /// Insert the report, or refresh it in place when one already exists for the compare
    /// (bumps GeneratedDate). Returns the report id.
    /// </summary>
    public const string Upsert =
        "UPDATE Report SET "
        + "CompareResultId = @CompareResultId, Name = @Name, Status = @Status, "
        + "RegressionScore = @RegressionScore, RiskLevel = @RiskLevel, "
        + "TotalRequest = @TotalRequest, UnchangedCount = @UnchangedCount, ChangedCount = @ChangedCount, "
        + "AddedCount = @AddedCount, RemovedCount = @RemovedCount, FailedCount = @FailedCount, "
        + "TotalApis = @TotalApis, BreakingApiCount = @BreakingApiCount, GeneratedDate = SYSUTCDATETIME() "
        + "WHERE CompareJobId = @CompareJobId; "
        + "IF @@ROWCOUNT = 0 "
        + "INSERT INTO Report (ProjectId, CompareJobId, CompareResultId, Name, Status, RegressionScore, RiskLevel, "
        + "TotalRequest, UnchangedCount, ChangedCount, AddedCount, RemovedCount, FailedCount, TotalApis, BreakingApiCount, "
        + "GeneratedDate, CreatedDate) "
        + "VALUES (@ProjectId, @CompareJobId, @CompareResultId, @Name, @Status, @RegressionScore, @RiskLevel, "
        + "@TotalRequest, @UnchangedCount, @ChangedCount, @AddedCount, @RemovedCount, @FailedCount, @TotalApis, @BreakingApiCount, "
        + "SYSUTCDATETIME(), SYSUTCDATETIME()); "
        + "SELECT Id FROM Report WHERE CompareJobId = @CompareJobId;";

    // ----- Delete (report + its stored AI analysis; compare/runs are upstream and untouched) -----

    /// <summary>
    /// Deletes a report's stored AI analysis then the report itself (FK-safe order: child first, since
    /// ReportAiAnalysis → Report has no cascade). Run within a transaction by the repository.
    /// </summary>
    public const string Delete =
        "DELETE FROM ReportAiAnalysis WHERE ReportId = @Id;\n"
        + "DELETE FROM Report WHERE Id = @Id;";

    // ----- Query: Reports list (column order matches ReportListItem) -----

    public const string GetByProject =
        "SELECT r.Id, r.CompareJobId, cj.RunAId, cj.RunBId, ra.Title AS RunATitle, rb.Title AS RunBTitle, "
        + "r.Name, r.Status, r.RegressionScore, r.RiskLevel, "
        + "r.TotalRequest, r.UnchangedCount, r.ChangedCount, r.AddedCount, r.RemovedCount, r.FailedCount, "
        + "r.TotalApis, r.BreakingApiCount, r.GeneratedDate, r.CreatedDate "
        + "FROM Report r "
        + "INNER JOIN CompareJob cj ON cj.Id = r.CompareJobId "
        + "INNER JOIN Run ra ON ra.Id = cj.RunAId "
        + "INNER JOIN Run rb ON rb.Id = cj.RunBId "
        + "WHERE r.ProjectId = @ProjectId "
        + "ORDER BY r.GeneratedDate DESC, r.Id DESC;";

    // ----- Query: report detail row (column order matches ReportSummary) -----

    public const string GetById =
        "SELECT r.Id, r.ProjectId, r.CompareJobId, r.CompareResultId, cj.RunAId, cj.RunBId, "
        + "ra.Title AS RunATitle, rb.Title AS RunBTitle, "
        + "r.Name, r.Status, r.RegressionScore, r.RiskLevel, "
        + "r.TotalRequest, r.UnchangedCount, r.ChangedCount, r.AddedCount, r.RemovedCount, r.FailedCount, "
        + "r.TotalApis, r.BreakingApiCount, r.GeneratedDate, r.CreatedDate "
        + "FROM Report r "
        + "INNER JOIN CompareJob cj ON cj.Id = r.CompareJobId "
        + "INNER JOIN Run ra ON ra.Id = cj.RunAId "
        + "INNER JOIN Run rb ON rb.Id = cj.RunBId "
        + "WHERE r.Id = @Id;";

    // ----- Query: per-request raw duration + size (tagged with API) for the perf-stats endpoint -----

    /// <summary>
    /// One row per request with its API + each run's duration + response size (column order matches
    /// ReportPerfRow). A request present in only one run leaves the other run's values NULL.
    /// </summary>
    public const string GetPerformanceRows =
        "SELECT ar.Id AS RequestId, ar.ApiId, MAX(a.Name) AS ApiName, MAX(a.Method) AS Method, "
        + "MAX(CASE WHEN rr.RunId = @RunAId THEN rr.DurationMs END) AS DurationMsA, "
        + "MAX(CASE WHEN rr.RunId = @RunBId THEN rr.DurationMs END) AS DurationMsB, "
        + "MAX(CASE WHEN rr.RunId = @RunAId THEN rr.ResponseSize END) AS SizeA, "
        + "MAX(CASE WHEN rr.RunId = @RunBId THEN rr.ResponseSize END) AS SizeB "
        + "FROM RunResponse rr "
        + "INNER JOIN ApiRequest ar ON ar.Id = rr.RequestId "
        + "INNER JOIN Api a ON a.Id = ar.ApiId "
        + "WHERE rr.RunId IN (@RunAId, @RunBId) "
        + "GROUP BY ar.Id, ar.ApiId;";

    // ----- Query: response-time comparison (two result sets: per-request, then per-API) -----

    /// <summary>
    /// Per-request durations paired across the two runs (column order matches ReportRequestTiming),
    /// then per-API average durations (column order matches ReportApiTiming). Read via QueryMultiple.
    /// A request/API present in only one run leaves the other run's value NULL.
    /// </summary>
    public const string GetPerformance =
        "SELECT ar.Id AS RequestId, MAX(a.Id) AS ApiId, MAX(a.Name) AS ApiName, "
        + "MAX(ar.Title) AS Title, MAX(a.Method) AS Method, MAX(a.Endpoint) AS Endpoint, "
        + "AVG(CASE WHEN rr.RunId = @RunAId THEN rr.DurationMs END) AS DurationMsA, "
        + "AVG(CASE WHEN rr.RunId = @RunBId THEN rr.DurationMs END) AS DurationMsB "
        + "FROM RunResponse rr "
        + "INNER JOIN ApiRequest ar ON ar.Id = rr.RequestId "
        + "INNER JOIN Api a ON a.Id = ar.ApiId "
        + "WHERE rr.RunId IN (@RunAId, @RunBId) "
        + "GROUP BY ar.Id "
        + "ORDER BY ar.Id; "
        + "SELECT a.Id AS ApiId, MAX(a.Name) AS ApiName, MAX(a.Method) AS Method, "
        + "AVG(CASE WHEN rr.RunId = @RunAId THEN rr.DurationMs END) AS AvgDurationMsA, "
        + "AVG(CASE WHEN rr.RunId = @RunBId THEN rr.DurationMs END) AS AvgDurationMsB "
        + "FROM RunResponse rr "
        + "INNER JOIN ApiRequest ar ON ar.Id = rr.RequestId "
        + "INNER JOIN Api a ON a.Id = ar.ApiId "
        + "WHERE rr.RunId IN (@RunAId, @RunBId) "
        + "GROUP BY a.Id "
        + "ORDER BY MAX(a.Name);";
}
