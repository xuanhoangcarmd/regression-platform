namespace RegressionPlatform.Application.Features.Compare.Contracts;

/// <summary>
/// Full per-request diff for the side-by-side viewer: the field-level diff plus both runs'
/// canonical responses. NOTE: column order must match this constructor (Dapper binds positionally).
/// </summary>
public sealed record CompareDetailDiffResponse(
    long RequestId,
    string Title,
    string Method,
    string Endpoint,
    string Result,
    /// <summary>Field-level differences as a JSON array; null when there are none.</summary>
    string? DiffJson,
    int? StatusCodeA,
    int? StatusCodeB,
    /// <summary>Run A canonical response body, full (real values) — for display.</summary>
    string? ResponseAJson,
    /// <summary>Run B canonical response body, full (real values) — for display.</summary>
    string? ResponseBJson,
    /// <summary>Run A body with the current ignore rules applied — used to mark ignored lines.</summary>
    string? ResponseAComparedJson,
    /// <summary>Run B body with the current ignore rules applied — used to mark ignored lines.</summary>
    string? ResponseBComparedJson,
    string? ErrorMessageA,
    string? ErrorMessageB,
    /// <summary>Run A's side is its edited (expected) response, not the captured one.</summary>
    bool IsEditedA,
    /// <summary>Run B's side is its edited (expected) response, not the captured one.</summary>
    bool IsEditedB
);
