namespace RegressionPlatform.Application.Features.Runs.Contracts;

/// <summary>Response DTO for a regression run.</summary>
public sealed record RunResponse(
    long Id,
    string Title,
    string? Description,
    string Environment,
    string RunType,
    string Status,
    int? RequestLimitPerApi,
    int? TotalRequest,
    /// <summary>Requests executed so far (live, while Running); null otherwise.</summary>
    int? ExecutedRequest,
    int? PassCount,
    int? WarningCount,
    int? FailedCount,
    long? DurationMs,
    DateTime CreatedDate,
    /// <summary>Live % while Running (from the in-memory tracker); null otherwise.</summary>
    int? ProgressPercent,
    /// <summary>True if this run is used (as baseline/current) in any comparison — then it can't be deleted.</summary>
    bool HasCompare
);
