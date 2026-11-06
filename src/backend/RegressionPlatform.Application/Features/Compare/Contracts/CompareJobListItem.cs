namespace RegressionPlatform.Application.Features.Compare.Contracts;

/// <summary>
/// A compare job row for the Compare History list. Summary counts come from the linked
/// CompareResult and are null until the job finishes. NOTE: column order must match this
/// constructor (Dapper binds positionally).
/// </summary>
public sealed record CompareJobListItem(
    long Id,
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
    DateTime? FinishedDate,
    DateTime CreatedDate,
    /// <summary>True when a report was generated from this compare (so it can't be deleted).</summary>
    bool HasReport
);
