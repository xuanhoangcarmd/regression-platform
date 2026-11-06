using RegressionPlatform.Application.Features.Report.Ai.Contracts;

namespace RegressionPlatform.Application.Features.Report.Ai;

/// <summary>
/// An AI provider that turns a report's data into an analysis. Implementations live in Infrastructure
/// (RuleBased, DeepSeek, Gemini, Claude, OpenAI). The active one is chosen by <c>AI:Provider</c>.
/// </summary>
public interface IAiProvider
{
    /// <summary>Provider name (matches <see cref="Contracts.AiProviderNames"/> / config).</summary>
    string Name { get; }

    /// <summary>Analyzes the report. May call an external LLM; honour the cancellation token.</summary>
    Task<ReportAiAnalysisResult> AnalyzeReportAsync(ReportAiContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Answers a single user question about the report ("Ask AI"), scoped strictly to the report's
    /// data. Returns either a final answer or a request for one whitelisted drill-down source (the
    /// caller fulfils it and calls again). May call an external LLM; honour the cancellation token.
    /// </summary>
    Task<ReportAiAnswer> AskAsync(ReportAiAskRequest request, CancellationToken cancellationToken);
}
