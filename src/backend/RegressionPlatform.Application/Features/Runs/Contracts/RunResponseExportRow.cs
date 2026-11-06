namespace RegressionPlatform.Application.Features.Runs.Contracts;

/// <summary>
/// One executed request of a run for the Excel export: API identity, the request's input, the full
/// response body and timing. NOTE: column order must match this constructor (Dapper binds positionally).
/// </summary>
public sealed record RunResponseExportRow(
    long RequestId,
    string ApiName,
    string Method,
    string Endpoint,
    string Title,
    string? RequestQueryJson,
    string? RequestBody,
    string? ResponseJson,
    int? StatusCode,
    int? DurationMs,
    string? ErrorMessage
);
