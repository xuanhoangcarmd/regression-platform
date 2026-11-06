namespace RegressionPlatform.API.Contracts;

/// <summary>
/// Request body for creating a run. Empty/omitted <c>ApiIds</c> = run all of the dataset's APIs.
/// </summary>
public sealed record CreateRunRequest(
    int DatasetId,
    string Title,
    string? Description,
    string Environment,
    string RunType,
    int? RequestLimitPerApi,
    IReadOnlyList<int>? ApiIds,
    int? MaxParallelApis,
    int? MaxParallelRequestsPerApi
);
