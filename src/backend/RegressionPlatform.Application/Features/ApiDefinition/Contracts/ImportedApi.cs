namespace RegressionPlatform.Application.Features.ApiDefinition.Contracts;

/// <summary>An API operation parsed from an OpenAPI/Swagger document, ready to import.</summary>
public sealed record ImportedApi(string Name, string Method, string Endpoint, string? Description);
