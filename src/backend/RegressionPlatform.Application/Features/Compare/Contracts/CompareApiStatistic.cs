namespace RegressionPlatform.Application.Features.Compare.Contracts;

/// <summary>Per-API breakdown of a compare ("Change by API"). NOTE: column order must match this constructor.</summary>
public sealed record CompareApiStatistic(
    int ApiId,
    string ApiName,
    string Method,
    string Endpoint,
    int RequestCount,
    int UnchangedCount,
    int ChangedCount,
    int AddedCount,
    int RemovedCount,
    int FailedCount
);
