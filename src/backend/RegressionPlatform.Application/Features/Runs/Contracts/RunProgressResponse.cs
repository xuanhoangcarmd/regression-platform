namespace RegressionPlatform.Application.Features.Runs.Contracts;

/// <summary>
/// Lightweight live-progress snapshot for a single run, returned by the progress poll endpoint.
/// While Running, ExecutedRequest/TotalRequest/ProgressPercent come from the in-memory tracker;
/// once finished, Status flips and the final Pass/Warning/Failed counts + duration are populated.
/// </summary>
public sealed record RunProgressResponse(
    long Id,
    string Status,
    int? TotalRequest,
    /// <summary>Requests executed so far (live, while Running); null otherwise.</summary>
    int? ExecutedRequest,
    int? PassCount,
    int? WarningCount,
    int? FailedCount,
    long? DurationMs,
    /// <summary>Live % while Running (from the in-memory tracker); null otherwise.</summary>
    int? ProgressPercent
);
