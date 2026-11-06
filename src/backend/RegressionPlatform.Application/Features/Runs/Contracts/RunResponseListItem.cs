namespace RegressionPlatform.Application.Features.Runs.Contracts;

/// <summary>One executed request in a run (a RunResponse joined to its request + API).</summary>
public sealed record RunResponseListItem(
    long RequestId,
    string Title,
    string Method,
    string Endpoint,
    int? StatusCode,
    int? DurationMs,
    int? ResponseSize,
    string Status,
    string? ErrorMessage,
    DateTime CreatedDate
);

/// <summary>Filter/paging/sort inputs for a run's response list.</summary>
public sealed record RunResponseListParameters(
    long RunId,
    string? Search,
    string? Status,
    int PageNumber,
    int PageSize,
    string? SortBy = null,
    string? SortDirection = null,
    int? ApiId = null
);

/// <summary>
/// Full detail of one executed request in a run (Results tab): response body + response headers,
/// plus the request that was sent (headers / query / body, from the ApiRequest definition).
/// NOTE: column order must match this constructor — Dapper binds positionally.
/// </summary>
public sealed record RunResponseDetail(
    long RequestId,
    string Title,
    string Method,
    string Endpoint,
    int? StatusCode,
    int? DurationMs,
    int? ResponseSize,
    string Status,
    string? ErrorMessage,
    DateTime CreatedDate,
    string? ResponseJson,
    string? ResponseHeadersJson,
    string? RequestHeadersJson,
    string? RequestQueryJson,
    string? RequestBody
);
