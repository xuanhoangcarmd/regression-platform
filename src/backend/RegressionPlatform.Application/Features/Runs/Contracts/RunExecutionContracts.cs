namespace RegressionPlatform.Application.Features.Runs.Contracts;

/// <summary>A request to execute within a run (joined to its API for method + endpoint).</summary>
public sealed record RunnableRequest(
    long RequestId,
    int ApiId,
    string Method,
    string Endpoint,
    string? HeadersJson,
    string? QueryStringJson,
    string? RequestBody
);

/// <summary>An active ignore rule: which JSON path to drop, and which APIs it applies to (empty = all).</summary>
public sealed record IgnoreRulePath(string JsonPath, IReadOnlyList<int> ApiIds)
{
    public bool AppliesToAll => ApiIds.Count == 0;

    public bool AppliesTo(int apiId) => AppliesToAll || ApiIds.Contains(apiId);
}

/// <summary>A TrimAfter rule: string value(s) at JsonPath are compared only up to Delimiter. Empty ApiIds = all APIs.</summary>
public sealed record TrimRulePath(string JsonPath, string Delimiter, IReadOnlyList<int> ApiIds)
{
    public bool AppliesTo(int apiId) => ApiIds.Count == 0 || ApiIds.Contains(apiId);
}

/// <summary>Minimal run info the executor needs. <c>RequestLimitPerApi</c> null = no cap.</summary>
public sealed record RunExecutionInfo(
    long Id,
    int ProjectId,
    int DatasetId,
    string Environment,
    int? RequestLimitPerApi,
    int MaxParallelApis,
    int MaxParallelRequestsPerApi
);
