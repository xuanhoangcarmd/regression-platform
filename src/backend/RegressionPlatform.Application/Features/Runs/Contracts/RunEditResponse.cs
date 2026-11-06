namespace RegressionPlatform.Application.Features.Runs.Contracts;

/// <summary>Editable fields of a (Pending) run, used to prefill the edit form.</summary>
public sealed record RunEditResponse(
    long Id,
    int DatasetId,
    string Title,
    string? Description,
    string Environment,
    string RunType,
    int? RequestLimitPerApi,
    int MaxParallelApis,
    int MaxParallelRequestsPerApi,
    /// <summary>Selected API ids; empty = the run targets all of the dataset's APIs.</summary>
    IReadOnlyList<int> ApiIds
);
