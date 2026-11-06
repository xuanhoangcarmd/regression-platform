using RegressionPlatform.Application.Features.Report.Ai.Contracts;
using RegressionPlatform.Application.Features.Report.Contracts;

namespace RegressionPlatform.Application.Features.Report.Ai;

/// <summary>
/// Builds the AI context for a report and runs the configured provider (falling back to rule-based
/// on failure). Shared by the read (generate-if-missing) and the explicit regenerate paths.
/// </summary>
public sealed class ReportAiRunner(
    ReportAiContextBuilder contextBuilder,
    IAiProviderResolver providerResolver
)
{
    /// <summary>Generates a fresh analysis (does not persist or cache).</summary>
    public async Task<ReportAiAnalysisResult> RunAsync(ReportSummary report, CancellationToken cancellationToken)
    {
        var context = await contextBuilder.BuildAsync(report, cancellationToken);

        var provider = providerResolver.Resolve();
        try
        {
            return await provider.AnalyzeReportAsync(context, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The configured provider failed (no key, network, bad response) — degrade gracefully to
            // the deterministic rule-based provider so the tab always renders something.
            return await providerResolver.Fallback().AnalyzeReportAsync(context, cancellationToken);
        }
    }
}
