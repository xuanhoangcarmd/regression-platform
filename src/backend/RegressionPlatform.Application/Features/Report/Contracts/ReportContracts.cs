namespace RegressionPlatform.Application.Features.Report.Contracts;

/// <summary>Lifecycle status of a <c>Report</c>.</summary>
public static class ReportStatuses
{
    public const string Completed = "Completed";
}

/// <summary>Risk levels for a report (matches <c>CK_Report_RiskLevel</c>).</summary>
public static class ReportRiskLevels
{
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string High = "High";
    public const string Critical = "Critical";
}

/// <summary>
/// A report row for the Reports list (report snapshot + its compare's run labels).
/// NOTE: column order must match this constructor (Dapper binds positionally).
/// </summary>
public sealed record ReportListItem(
    long Id,
    long CompareJobId,
    long RunAId,
    long RunBId,
    string RunATitle,
    string RunBTitle,
    string Name,
    string Status,
    decimal RegressionScore,
    string RiskLevel,
    int TotalRequest,
    int UnchangedCount,
    int ChangedCount,
    int AddedCount,
    int RemovedCount,
    int FailedCount,
    int TotalApis,
    int BreakingApiCount,
    DateTime GeneratedDate,
    DateTime CreatedDate
);

/// <summary>
/// Full report row incl. ownership/link fields (for the detail handler). NOTE: column order must
/// match this constructor (Dapper binds positionally).
/// </summary>
public sealed record ReportSummary(
    long Id,
    int ProjectId,
    long CompareJobId,
    long CompareResultId,
    long RunAId,
    long RunBId,
    string RunATitle,
    string RunBTitle,
    string Name,
    string Status,
    decimal RegressionScore,
    string RiskLevel,
    int TotalRequest,
    int UnchangedCount,
    int ChangedCount,
    int AddedCount,
    int RemovedCount,
    int FailedCount,
    int TotalApis,
    int BreakingApiCount,
    DateTime GeneratedDate,
    DateTime CreatedDate
);

/// <summary>Per-API breakdown for a report (compare stats + a breaking-change count for the API).</summary>
public sealed record ReportApiStat(
    int ApiId,
    string ApiName,
    string Method,
    string Endpoint,
    int RequestCount,
    int UnchangedCount,
    int ChangedCount,
    int AddedCount,
    int RemovedCount,
    int FailedCount,
    int BreakingCount
);

/// <summary>
/// Response time of a single request in each of the compared runs (paired by request).
/// NOTE: column order must match this constructor (Dapper binds positionally).
/// </summary>
public sealed record ReportRequestTiming(
    long RequestId,
    int ApiId,
    string ApiName,
    string Title,
    string Method,
    string Endpoint,
    int? DurationMsA,
    int? DurationMsB
);

/// <summary>
/// Average response time of an API in each of the compared runs.
/// NOTE: column order must match this constructor (Dapper binds positionally).
/// </summary>
public sealed record ReportApiTiming(
    int ApiId,
    string ApiName,
    string Method,
    int? AvgDurationMsA,
    int? AvgDurationMsB
);

/// <summary>
/// Response-time comparison for a report's two runs (drives the Distribution tab charts):
/// per-request durations and per-API averages, Run A vs Run B.
/// </summary>
public sealed record ReportPerformanceResponse(
    long RunAId,
    long RunBId,
    string RunATitle,
    string RunBTitle,
    IReadOnlyList<ReportRequestTiming> PerRequest,
    IReadOnlyList<ReportApiTiming> PerApi
);

/// <summary>
/// One request's raw duration + response size in each run (paired by request), tagged with its API —
/// the input the performance-stats handler reduces into the latency histogram + per-API table.
/// NOTE: column order must match this constructor (Dapper binds positionally).
/// </summary>
public sealed record ReportPerfRow(
    long RequestId,
    int ApiId,
    string ApiName,
    string Method,
    int? DurationMsA,
    int? DurationMsB,
    int? SizeA,
    int? SizeB
);

/// <summary>One bucket of the latency histogram (count of requests whose duration falls in it).</summary>
public sealed record ReportPerfBin(int FromMs, int ToMs, int CountA, int CountB);

/// <summary>Latency distribution: equal-width bins over the combined duration range.</summary>
public sealed record ReportPerfHistogram(int BinSizeMs, IReadOnlyList<ReportPerfBin> Bins);

/// <summary>
/// Per-API latency + size stats NOT shown elsewhere (p95 / max / avg size), plus the average that
/// backs the Δ% column (the avg values themselves are shown by the existing "Avg by API" bar).
/// </summary>
public sealed record ReportPerfApiStat(
    int ApiId,
    string ApiName,
    string Method,
    int? P95MsA, int? P95MsB,
    int? MaxMsA, int? MaxMsB,
    int? MinMsA, int? MinMsB,
    long? AvgSizeA, long? AvgSizeB,
    int? AvgMsA, int? AvgMsB
);

/// <summary>Extra performance statistics for the Distribution tab (latency histogram + per-API table).</summary>
public sealed record ReportPerformanceStatsResponse(
    string RunATitle,
    string RunBTitle,
    ReportPerfHistogram Histogram,
    IReadOnlyList<ReportPerfApiStat> PerApi
);


/// <summary>
/// Report detail (Summary tab): the report snapshot header + the compare's per-API breakdown
/// (the client derives the Top-5-by-change% and the distribution from these).
/// </summary>
public sealed record ReportDetailResponse(
    long Id,
    long CompareJobId,
    long RunAId,
    long RunBId,
    string RunATitle,
    string RunBTitle,
    string Name,
    string Status,
    decimal RegressionScore,
    string RiskLevel,
    int TotalRequest,
    int UnchangedCount,
    int ChangedCount,
    int AddedCount,
    int RemovedCount,
    int FailedCount,
    int TotalApis,
    int BreakingApiCount,
    DateTime GeneratedDate,
    DateTime CreatedDate,
    IReadOnlyList<ReportApiStat> Apis
);
