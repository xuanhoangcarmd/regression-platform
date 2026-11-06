namespace RegressionPlatform.Domain.Entities;

/// <summary>
/// A field-ignore rule for response comparison. Mirrors the <c>CompareIgnoreRule</c> table
/// (docs/database/Tables.md). This table has no audit columns.
/// </summary>
public class CompareIgnoreRule
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string JsonPath { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Enabled { get; set; } = true;

    /// <summary>How the rule affects comparison. See <see cref="CompareIgnoreRuleTypes"/>.</summary>
    public string RuleType { get; set; } = CompareIgnoreRuleTypes.Ignore;

    /// <summary>
    /// TrimAfter only: compare a string value only up to (not including) the first occurrence of this
    /// text — e.g. "?" drops a signed URL's query string. Null for the other rule types.
    /// </summary>
    public string? Delimiter { get; set; }
}

/// <summary>The kinds of compare rule stored in <see cref="CompareIgnoreRule"/>.</summary>
public static class CompareIgnoreRuleTypes
{
    /// <summary>Drop the field at JsonPath before comparing (default, original behavior).</summary>
    public const string Ignore = "Ignore";

    /// <summary>Lift the subtree at JsonPath to the response root before comparing (envelope unwrap).</summary>
    public const string ExtractRoot = "ExtractRoot";

    /// <summary>Keep a string value only up to the first <see cref="CompareIgnoreRule.Delimiter"/> before comparing.</summary>
    public const string TrimAfter = "TrimAfter";

    public static bool IsValid(string? value) => value is Ignore or ExtractRoot or TrimAfter;
}
