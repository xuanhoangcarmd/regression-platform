using MediatR;
using RegressionPlatform.Application.Features.ApiDefinition.Contracts;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Domain.Entities;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.ApiDefinition.Commands.ImportApis;

public sealed class ImportApisFromSwaggerCommandHandler(
    IApiSpecImporter importer,
    IApiRepository repository,
    IProjectRepository projectRepository
) : IRequestHandler<ImportApisFromSwaggerCommand, Result<ImportApisResponse>>
{
    public async Task<Result<ImportApisResponse>> Handle(
        ImportApisFromSwaggerCommand request,
        CancellationToken cancellationToken
    )
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<ImportApisResponse>.NotFound(
                new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist.")
            );
        }

        IReadOnlyList<ImportedApi> parsed;
        try
        {
            parsed = await importer.FetchAsync(request.SwaggerUrl, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result<ImportApisResponse>.Failure(
                new Error("Swagger.ImportFailed", $"Failed to download or read the Swagger document: {ex.Message}")
            );
        }

        var total = parsed.Count;

        // Keep only operations valid against the Api schema (allowed method + lengths).
        var candidates = parsed.Where(item =>
            ApiHttpMethods.Allowed.Contains(item.Method)
            && !string.IsNullOrWhiteSpace(item.Name)
            && item.Name.Length <= 200
            && !string.IsNullOrWhiteSpace(item.Endpoint)
            && item.Endpoint.Length <= 500
        );

        // Dedup by Method + Endpoint, against the project's APIs and within this batch.
        var existingKeys = await repository.GetMethodEndpointKeysAsync(projectId.Value, cancellationToken);
        var seen = new HashSet<string>(existingKeys, StringComparer.Ordinal);

        // Name is unique per project — track used names to avoid collisions (summaries can repeat).
        var existingNames = await repository.GetNamesAsync(projectId.Value, cancellationToken);
        var usedNames = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);

        var toInsert = new List<Api>();
        foreach (var item in candidates)
        {
            if (!seen.Add($"{item.Method}|{item.Endpoint}"))
            {
                continue;
            }
            toInsert.Add(
                new Api
                {
                    ProjectId = projectId.Value,
                    Name = MakeUniqueName(item, usedNames),
                    Method = item.Method,
                    Endpoint = item.Endpoint,
                    Description = item.Description,
                    IsActive = true,
                }
            );
        }

        var imported = toInsert.Count == 0 ? 0 : await repository.CreateManyAsync(toInsert, cancellationToken);

        return Result<ImportApisResponse>.Success(new ImportApisResponse(total, imported, total - imported));
    }

    /// <summary>
    /// Returns a name not already used. On collision, qualifies with "(METHOD endpoint)" — which is
    /// unique because the batch is deduped by method+endpoint — and adds the name to <paramref name="usedNames"/>.
    /// </summary>
    private static string MakeUniqueName(ImportedApi item, HashSet<string> usedNames)
    {
        if (usedNames.Add(item.Name))
        {
            return item.Name;
        }

        var qualified = Truncate($"{item.Name} ({item.Method} {item.Endpoint})", 200);
        var attempt = 1;
        while (!usedNames.Add(qualified))
        {
            qualified = Truncate($"{item.Name} ({item.Method} {item.Endpoint}) {attempt++}", 200);
        }
        return qualified;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
