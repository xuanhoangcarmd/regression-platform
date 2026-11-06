namespace RegressionPlatform.API.Contracts;

/// <summary>Request body for creating an API definition.</summary>
public sealed record CreateApiRequest(string Name, string Method, string Endpoint, string? Description, bool IsActive);

/// <summary>Request body for updating an API definition (id comes from the route).</summary>
public sealed record UpdateApiRequest(string Name, string Method, string Endpoint, string? Description, bool IsActive);

/// <summary>Request body for importing API definitions from an OpenAPI/Swagger URL.</summary>
public sealed record ImportApisRequest(string SwaggerUrl);
