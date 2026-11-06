namespace RegressionPlatform.API.Contracts;

/// <summary>Request body for creating a request in a dataset.</summary>
public sealed record CreateApiRequestBody(
    int DatasetId,
    int ApiId,
    string Title,
    string? Description,
    string? HeadersJson,
    string? QueryStringJson,
    string? RequestBody
);

/// <summary>Request body for updating a pooled request (creates a new version; id from the route).</summary>
public sealed record UpdateApiRequestBody(
    int ApiId,
    string Title,
    string? Description,
    string? HeadersJson,
    string? QueryStringJson,
    string? RequestBody
);
