using RegressionPlatform.Application.Features.ApiDefinition.Contracts;

namespace RegressionPlatform.Application.Features.ApiDefinition;

/// <summary>
/// Fetches and parses an OpenAPI/Swagger document into importable API operations.
/// Implemented in Infrastructure (HTTP + OpenAPI parsing).
/// </summary>
public interface IApiSpecImporter
{
    Task<IReadOnlyList<ImportedApi>> FetchAsync(string swaggerUrl, CancellationToken cancellationToken);
}
