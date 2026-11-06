namespace RegressionPlatform.Application.Features.Report.Ai.Contracts;

/// <summary>Known AI provider names (matches <c>AI:Provider</c> config + the provider classes).</summary>
public static class AiProviderNames
{
    public const string RuleBased = "RuleBased";
    public const string DeepSeek = "DeepSeek";
    public const string Gemini = "Gemini";
    public const string Claude = "Claude";
    public const string OpenAI = "OpenAI";
}

/// <summary>Per-API signal fed to the AI provider.</summary>
public sealed record ReportAiApiContext(
    string ApiName,
    string Method,
    string Endpoint,
    int RequestCount,
    int ChangedCount,
    int AddedCount,
    int RemovedCount,
    int FailedCount,
    int BreakingCount
);

/// <summary>A distinct field-change pattern fed to the AI provider (for semantic hints).</summary>
public sealed record ReportAiChangePattern(string ApiName, string ChangeType, string Path, int Occurrences);

/// <summary>
/// Everything the AI provider needs to analyze a report: the snapshot headline, per-API breakdown,
/// notable field-change patterns and a couple of derived deltas. Pure data — no IO.
/// </summary>
public sealed record ReportAiContext(
    string Name,
    string RunATitle,
    string RunBTitle,
    decimal RegressionScore,
    string RiskLevel,
    int TotalRequest,
    int UnchangedCount,
    int ChangedCount,
    int AddedCount,
    int RemovedCount,
    int FailedCount,
    int TotalApis,
    int BreakingApiCount,
    int FieldsRemoved,
    double ResponseTimeDeltaPercent,
    IReadOnlyList<ReportAiApiContext> Apis,
    IReadOnlyList<ReportAiChangePattern> ChangePatterns
);

/// <summary>
/// AI analysis of a report (Executive Summary / Risk Assessment / Recommendations / example prompts).
/// Returned by an <see cref="IAiProvider"/>; shape mirrors the UI's AI Analysis tab.
/// </summary>
public sealed record ReportAiAnalysisResult(
    string Summary,
    IReadOnlyList<string> Findings,
    string Recommendation,
    string RiskLevel,
    int RiskScore,
    IReadOnlyList<string> RiskFactors,
    IReadOnlyList<string> Recommendations,
    IReadOnlyList<string> ExampleQuestions,
    string Provider,
    /// <summary>When this analysis was generated/persisted (null until stored).</summary>
    DateTime? GeneratedAt = null
);
