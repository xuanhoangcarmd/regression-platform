using RegressionPlatform.Application.Features.Report.Ai;
using RegressionPlatform.Application.Features.Report.Ai.Contracts;

namespace RegressionPlatform.Infrastructure.Ai.Providers;

// Placeholder providers so the IAiProvider tree is complete. Only DeepSeek (and the rule-based
// fallback) are implemented today; selecting one of these throws, and the query handler degrades
// to the rule-based provider.

/// <summary>Google Gemini provider — not implemented yet.</summary>
public sealed class GeminiProvider : IAiProvider
{
    public string Name => AiProviderNames.Gemini;

    public Task<ReportAiAnalysisResult> AnalyzeReportAsync(ReportAiContext context, CancellationToken cancellationToken) =>
        throw new NotSupportedException("The 'Gemini' AI provider is not implemented yet.");

    public Task<ReportAiAnswer> AskAsync(ReportAiAskRequest request, CancellationToken cancellationToken) =>
        throw new NotSupportedException("The 'Gemini' AI provider is not implemented yet.");
}

/// <summary>Anthropic Claude provider — not implemented yet.</summary>
public sealed class ClaudeProvider : IAiProvider
{
    public string Name => AiProviderNames.Claude;

    public Task<ReportAiAnalysisResult> AnalyzeReportAsync(ReportAiContext context, CancellationToken cancellationToken) =>
        throw new NotSupportedException("The 'Claude' AI provider is not implemented yet.");

    public Task<ReportAiAnswer> AskAsync(ReportAiAskRequest request, CancellationToken cancellationToken) =>
        throw new NotSupportedException("The 'Claude' AI provider is not implemented yet.");
}

/// <summary>OpenAI provider — not implemented yet.</summary>
public sealed class OpenAiProvider : IAiProvider
{
    public string Name => AiProviderNames.OpenAI;

    public Task<ReportAiAnalysisResult> AnalyzeReportAsync(ReportAiContext context, CancellationToken cancellationToken) =>
        throw new NotSupportedException("The 'OpenAI' AI provider is not implemented yet.");

    public Task<ReportAiAnswer> AskAsync(ReportAiAskRequest request, CancellationToken cancellationToken) =>
        throw new NotSupportedException("The 'OpenAI' AI provider is not implemented yet.");
}
