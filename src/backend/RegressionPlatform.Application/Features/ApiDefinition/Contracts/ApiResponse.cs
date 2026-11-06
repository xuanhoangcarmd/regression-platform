namespace RegressionPlatform.Application.Features.ApiDefinition.Contracts;

/// <summary>Response DTO for an API definition. <c>RequestCount</c> is the number of ApiRequest rows.</summary>
public sealed record ApiResponse(
    int Id,
    string Name,
    string Method,
    string Endpoint,
    string? Description,
    bool IsActive,
    int RequestCount
);
