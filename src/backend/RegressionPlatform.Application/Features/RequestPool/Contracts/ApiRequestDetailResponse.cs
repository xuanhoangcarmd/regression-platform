namespace RegressionPlatform.Application.Features.RequestPool.Contracts;

/// <summary>Full ApiRequest detail (for editing/prefill).</summary>
public sealed record ApiRequestDetailResponse(
    long Id,
    int ApiId,
    string Title,
    string? Description,
    string? HeadersJson,
    string? QueryStringJson,
    string? RequestBody,
    int Version,
    bool IsActive
);
