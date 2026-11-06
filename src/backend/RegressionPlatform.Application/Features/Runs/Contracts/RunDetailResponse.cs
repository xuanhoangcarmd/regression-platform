namespace RegressionPlatform.Application.Features.Runs.Contracts;

/// <summary>Per-API execution statistics within a run.</summary>
public sealed record ApiRunStatistic(
    int ApiId,
    string ApiName,
    string Method,
    string Endpoint,
    int RequestCount,
    int PassedCount,
    int WarningCount,
    int FailedCount,
    double? AvgResponseTimeMs
);

/// <summary>Full detail for a single run (Summary tab).</summary>
public sealed record RunDetailResponse(
    long Id,
    string Title,
    string? Description,
    string Environment,
    string RunType,
    string Status,
    int? TotalRequest,
    int? PassCount,
    int? WarningCount,
    int? FailedCount,
    DateTime? StartTime,
    DateTime? EndTime,
    long? DurationMs,
    DateTime CreatedDate,
    int ApiCount,
    IReadOnlyList<ApiRunStatistic> ApiStatistics
);
