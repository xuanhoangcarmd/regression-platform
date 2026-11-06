namespace RegressionPlatform.API.Contracts;

/// <summary>Request body for creating an ignore rule. Empty/omitted <c>ApiIds</c> = applies to all APIs.
/// Omitted <c>RuleType</c> defaults to "Ignore".</summary>
public sealed record CreateIgnoreRuleRequest(
    string JsonPath,
    string? Description,
    bool Enabled,
    string? RuleType,
    IReadOnlyList<int>? ApiIds,
    string? Delimiter = null
);

/// <summary>Request body for updating an ignore rule (id from the route). Omitted <c>RuleType</c> defaults to "Ignore".</summary>
public sealed record UpdateIgnoreRuleRequest(
    string JsonPath,
    string? Description,
    bool Enabled,
    string? RuleType,
    IReadOnlyList<int>? ApiIds,
    string? Delimiter = null
);
