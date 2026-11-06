namespace RegressionPlatform.Application.Features.Compare.Contracts;

/// <summary>One per-request row in the compare detail list. NOTE: column order must match this constructor.</summary>
public sealed record CompareDetailListItem(
    long RequestId,
    string Title,
    string Method,
    string Endpoint,
    string Result,
    /// <summary>An edited (expected) response of either run was used for this request.</summary>
    bool IsEdited
);

/// <summary>
/// One per-request row for the full-detail export: identity + result + the raw field-level diff JSON
/// (null unless the request Changed). NOTE: column order must match this constructor (Dapper positional).
/// </summary>
public sealed record CompareFullDetailItem(
    long RequestId,
    string Title,
    string Method,
    string Endpoint,
    string Result,
    string? DiffJson,
    /// <summary>The request's input that was sent: query string params + body (for the full export).</summary>
    string? QueryStringJson,
    string? RequestBody
);

/// <summary>
/// One per-request row for the "Test Results" export: the request's identity + input, BOTH runs' raw
/// stored responses (status / duration / error / body) and the compare outcome with its field diff.
/// NOTE: column order must match this constructor (Dapper positional).
/// </summary>
public sealed record CompareTestResultItem(
    long RequestId,
    string ApiName,
    string Method,
    string Endpoint,
    string Title,
    string? Description,
    string? QueryStringJson,
    string? RequestBody,
    string Result,
    string? DiffJson,
    int? StatusCodeA,
    int? StatusCodeB,
    int? DurationMsA,
    int? DurationMsB,
    string? ErrorMessageA,
    string? ErrorMessageB,
    string? ResponseAJson,
    string? ResponseBJson,
    /// <summary>A run's columns come from its edited (expected) response where one exists.</summary>
    bool IsEdited
);

/// <summary>Filter/paging/sort inputs for a compare's per-request detail list.</summary>
public sealed record CompareDetailListParameters(
    long CompareResultId,
    string? Search,
    string? Result,
    int PageNumber,
    int PageSize,
    string? SortBy = null,
    string? SortDirection = null,
    int? ApiId = null,
    // Change-pattern filter (index-normalized path + change type) — matched against DiffJson.
    string? ChangeType = null,
    string? ChangePath = null
);
