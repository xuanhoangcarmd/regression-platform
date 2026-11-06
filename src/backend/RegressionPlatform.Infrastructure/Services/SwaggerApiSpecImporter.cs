using System.Text.Json;
using RegressionPlatform.Application.Features.ApiDefinition;
using RegressionPlatform.Application.Features.ApiDefinition.Contracts;

namespace RegressionPlatform.Infrastructure.Services;

/// <summary>
/// Fetches an OpenAPI/Swagger JSON document over HTTP and parses its `paths` with
/// System.Text.Json (works for v2 &amp; v3; no dependency on Microsoft.OpenApi).
/// </summary>
public sealed class SwaggerApiSpecImporter(HttpClient httpClient) : IApiSpecImporter
{
    private static readonly HashSet<string> HttpMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "get",
        "post",
        "put",
        "patch",
        "delete",
    };

    public async Task<IReadOnlyList<ImportedApi>> FetchAsync(string swaggerUrl, CancellationToken cancellationToken)
    {
        await using var stream = await httpClient.GetStreamAsync(swaggerUrl, cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var apis = new List<ImportedApi>();
        if (!document.RootElement.TryGetProperty("paths", out var paths) || paths.ValueKind != JsonValueKind.Object)
        {
            return apis;
        }

        foreach (var pathProperty in paths.EnumerateObject())
        {
            var endpoint = pathProperty.Name;
            if (pathProperty.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var operationProperty in pathProperty.Value.EnumerateObject())
            {
                if (!HttpMethods.Contains(operationProperty.Name))
                {
                    continue;
                }

                var method = operationProperty.Name.ToUpperInvariant();
                var operation = operationProperty.Value;

                // Prefer the operation's title (summary); fall back to "METHOD endpoint".
                var title = GetString(operation, "summary");
                var name = string.IsNullOrWhiteSpace(title) ? $"{method} {endpoint}" : title;
                var description = GetString(operation, "description");

                apis.Add(new ImportedApi(Truncate(name, 200), method, Truncate(endpoint, 500), TruncateOrNull(description, 1000)));
            }
        }

        return apis;
    }

    private static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private static string? TruncateOrNull(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : Truncate(value, maxLength);
}
