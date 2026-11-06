namespace RegressionPlatform.Application.Features.RequestPool.Contracts;

/// <summary>A row in the Request Pool list (ApiRequest joined to its Api for name + method).</summary>
public sealed record ApiRequestListItemResponse(
    long Id,
    int ApiId,
    string ApiName,
    string Title,
    string? Description,
    string Method,
    bool IsActive
);
