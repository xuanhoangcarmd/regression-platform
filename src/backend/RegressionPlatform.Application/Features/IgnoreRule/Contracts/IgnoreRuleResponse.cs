namespace RegressionPlatform.Application.Features.IgnoreRule.Contracts;

/// <summary>An API a rule applies to.</summary>
public sealed record IgnoreRuleApiRef(int Id, string Name);

/// <summary>
/// Response DTO for a compare ignore rule. <see cref="Apis"/> are the APIs the rule applies to;
/// an empty list means the rule applies to ALL APIs.
/// </summary>
public sealed record IgnoreRuleResponse(
    int Id,
    string JsonPath,
    string? Description,
    bool Enabled,
    /// <summary>"Ignore" (drop the field), "ExtractRoot" (lift the subtree to the response root) or
    /// "TrimAfter" (compare a string only up to <see cref="Delimiter"/>).</summary>
    string RuleType,
    /// <summary>TrimAfter only: the text at which the value is cut (e.g. "?").</summary>
    string? Delimiter,
    IReadOnlyList<IgnoreRuleApiRef> Apis
);
