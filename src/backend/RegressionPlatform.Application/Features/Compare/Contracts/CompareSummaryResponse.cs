namespace RegressionPlatform.Application.Features.Compare.Contracts;

/// <summary>
/// Compare detail header (job + result summary + run labels). Per-API breakdown and the
/// per-request list are separate queries. NOTE: column order must match this constructor.
/// </summary>
public sealed record CompareSummaryResponse(
    long Id,
    int ProjectId,
    long RunAId,
    long RunBId,
    string RunATitle,
    string RunBTitle,
    string Status,
    int Progress,
    long? CompareResultId,
    int? TotalRequest,
    int? UnchangedCount,
    int? ChangedCount,
    int? AddedCount,
    int? RemovedCount,
    int? FailedCount,
    DateTime? StartedDate,
    DateTime? FinishedDate,
    DateTime CreatedDate,
    /// <summary>Run A body source: 'Original' or 'Expected'.</summary>
    string BaselineMode,
    /// <summary>True = order-insensitive compare; false = strict order.</summary>
    bool IgnoreOrder
);
