using System.Text.Json;
using System.Text.Json.Nodes;
using MediatR;
using RegressionPlatform.Application.Features.ApiDefinition;
using RegressionPlatform.Application.Features.RequestPool.Contracts;
using RegressionPlatform.Domain.Entities;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.RequestPool.Commands.ImportApiRequests;

/// <summary>
/// Imports pooled requests from an uploaded Excel file. Each data row becomes one request and the
/// column headers are the field names. Fields are serialized into the query string (when the target
/// API is GET) or the request body (otherwise). A "Title" column is required and names each request;
/// rows with an empty title are skipped.
///
/// Nested payloads: a dotted column name ("VehicleBuffersRequest.Vin") nests the value inside that
/// object, and a cell whose text is itself JSON (starts with '{' or '[') is embedded as a real
/// object/array — so structured bodies (nested objects, arrays of strings, …) can be imported too.
/// An optional "Description" column is stored as the request's description (not part of the body).
/// An EMPTY cell omits the field from that row's body (rows with different field sets can share one
/// sheet); the literals <c>""</c> and <c>null</c> express an intentional empty string / JSON null.
/// </summary>
public sealed class ImportApiRequestsCommandHandler(
    IExcelRequestParser parser,
    IApiRepository apiRepository,
    IApiRequestRepository requestRepository
) : IRequestHandler<ImportApiRequestsCommand, Result<ImportApiRequestsResponse>>
{
    private const string TitleColumn = "Title";
    private const string DescriptionColumn = "Description";
    private const int MaxTitleLength = 300;
    private const int MaxDescriptionLength = 1000;

    public async Task<Result<ImportApiRequestsResponse>> Handle(
        ImportApiRequestsCommand request,
        CancellationToken cancellationToken
    )
    {
        var api = await apiRepository.GetByIdAsync(request.ApiId, cancellationToken);
        if (api is null)
        {
            return Result<ImportApiRequestsResponse>.NotFound(
                new Error("Api.NotFound", $"API {request.ApiId} does not exist.")
            );
        }

        ExcelImportData data;
        try
        {
            data = parser.Parse(request.File);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result<ImportApiRequestsResponse>.Failure(
                new Error("Excel.ParseFailed", $"Failed to read the Excel file: {ex.Message}")
            );
        }

        // A "Title" column (case-insensitive) is required — validate it up front, before looking at rows.
        var titleColumn = data.Columns.FirstOrDefault(column =>
            string.Equals(column, TitleColumn, StringComparison.OrdinalIgnoreCase)
        );
        if (titleColumn is null)
        {
            return Result<ImportApiRequestsResponse>.Failure(
                new Error(
                    "Excel.MissingTitleColumn",
                    $"The Excel file is missing the required column '{TitleColumn}'. Please add a '{TitleColumn}' column as the request title."
                )
            );
        }

        var total = data.Rows.Count;
        if (total == 0)
        {
            return Result<ImportApiRequestsResponse>.Success(new ImportApiRequestsResponse(0, 0, 0));
        }

        // GET requests carry their fields in the query string; everything else uses the body.
        var isQuery = string.Equals(api.Method, "GET", StringComparison.OrdinalIgnoreCase);
        // An optional "Description" column (case-insensitive) is metadata, never a body field.
        var descriptionColumn = data.Columns.FirstOrDefault(column =>
            string.Equals(column, DescriptionColumn, StringComparison.OrdinalIgnoreCase)
        );
        var fieldColumns = data.Columns.Where(column => column != titleColumn && column != descriptionColumn).ToList();

        var toInsert = new List<ApiRequest>(total);
        foreach (var row in data.Rows)
        {
            // Title is required per row; skip rows without one.
            var title = row.TryGetValue(titleColumn, out var titleValue) ? titleValue?.ToString() : null;
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            // Cell values are already typed by the parser (number/bool/string per the Excel cell
            // format), so they serialize with the right JSON type (e.g. a numeric cell -> 1, not "1").
            // Dotted column names nest ("A.B" -> {"A":{"B":…}}) and JSON-text cells embed as real
            // objects/arrays, so structured bodies import correctly.
            var description = descriptionColumn is not null && row.TryGetValue(descriptionColumn, out var descriptionValue)
                ? descriptionValue?.ToString()?.Trim()
                : null;

            var root = new JsonObject();
            foreach (var column in fieldColumns)
            {
                var value = row.TryGetValue(column, out var cell) ? cell : null;
                // An EMPTY cell means "this row doesn't have the field" (so one sheet can mix rows
                // with different field sets). The literals "" and null express an intentional empty
                // string / JSON null.
                if (value is null || (value is string text && text.Length == 0))
                {
                    continue;
                }
                SetNestedValue(root, column, ToJsonNode(value));
            }
            var fieldsJson = root.ToJsonString();

            toInsert.Add(
                new ApiRequest
                {
                    DatasetId = request.DatasetId,
                    ApiId = request.ApiId,
                    Title = Truncate(title.Trim(), MaxTitleLength),
                    Description = string.IsNullOrEmpty(description) ? null : Truncate(description, MaxDescriptionLength),
                    QueryStringJson = isQuery ? fieldsJson : null,
                    RequestBody = isQuery ? null : fieldsJson,
                    Version = 1,
                    IsActive = true,
                }
            );
        }

        var imported = await requestRepository.CreateManyAsync(toInsert, cancellationToken);
        return Result<ImportApiRequestsResponse>.Success(
            new ImportApiRequestsResponse(total, imported, total - imported)
        );
    }

    private static string Truncate(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];

    /// <summary>
    /// Sets a value at a dotted path inside <paramref name="root"/>, creating intermediate objects —
    /// "VehicleBuffersRequest.Vin" → { "VehicleBuffersRequest": { "Vin": … } }. A non-object already at
    /// an intermediate segment is replaced by an object (the dotted column wins).
    /// </summary>
    private static void SetNestedValue(JsonObject root, string path, JsonNode? value)
    {
        var segments = path.Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return;
        }

        var current = root;
        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (current[segments[i]] is JsonObject child)
            {
                current = child;
            }
            else
            {
                var created = new JsonObject();
                current[segments[i]] = created;
                current = created;
            }
        }
        current[segments[^1]] = value;
    }

    /// <summary>
    /// A typed cell value → JsonNode. A string that is itself JSON (starts with '{' or '[') is embedded
    /// as a real object/array; anything else keeps its primitive type from the Excel cell.
    /// </summary>
    private static JsonNode? ToJsonNode(object? value)
    {
        if (value is string text)
        {
            var trimmed = text.TrimStart();
            if (trimmed.Trim() == "null")
            {
                return null; // the JSON null literal — lets a cell express "field: null"
            }
            if (trimmed.Trim() == "\"\"")
            {
                return JsonValue.Create(string.Empty); // the "" literal — an intentional empty string
            }
            if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
            {
                try
                {
                    return JsonNode.Parse(text);
                }
                catch (JsonException)
                {
                    // Not valid JSON after all — keep it as the literal string.
                }
            }
            return JsonValue.Create(text);
        }

        return value switch
        {
            null => JsonValue.Create(string.Empty),
            long integral => JsonValue.Create(integral),
            double floating => JsonValue.Create(floating),
            bool boolean => JsonValue.Create(boolean),
            _ => JsonValue.Create(value.ToString()),
        };
    }
}
