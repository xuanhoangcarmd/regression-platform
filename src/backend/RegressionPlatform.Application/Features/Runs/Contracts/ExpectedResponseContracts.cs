namespace RegressionPlatform.Application.Features.Runs.Contracts;

/// <summary>
/// One API of a run with how many of its responses have an edited (expected) version.
/// NOTE: column order must match this constructor (Dapper positional).
/// </summary>
public sealed record ExpectedApiItem(
    int ApiId,
    string ApiName,
    string Method,
    string Endpoint,
    int RequestCount,
    int EditedCount
);

/// <summary>
/// One executed request of a run in the Expected Response editor list (no bodies).
/// NOTE: column order must match this constructor (Dapper positional).
/// </summary>
public sealed record ExpectedRequestItem(
    long RequestId,
    int ApiId,
    string Title,
    string? Description,
    /// <summary>Effective status code (the edited one when set, else the captured one).</summary>
    int? StatusCode,
    bool IsEdited,
    string? EditedBy,
    DateTime? EditedDate
);

/// <summary>The saved expected (edited) version of a response.</summary>
public sealed record ExpectedOverride(
    /// <summary>Null = the original status code is kept.</summary>
    int? StatusCode,
    string ResponseJson,
    string? Reason,
    string? UpdatedBy,
    DateTime UpdatedDate
);

/// <summary>Original captured response + its saved expected version (if any) for one request.</summary>
public sealed record ExpectedResponseDetail(
    long RequestId,
    int ApiId,
    string Title,
    int? StatusCode,
    string? ResponseJson,
    string? ErrorMessage,
    ExpectedOverride? Override
);

/// <summary>One edited response to save (upsert).</summary>
public sealed record ExpectedResponseSaveItem(long RequestId, int? StatusCode, string ResponseJson, string? Reason);
