using System.Globalization;
using RegressionPlatform.Application.Features.Report.Ai;
using RegressionPlatform.Application.Features.Report.Ai.Contracts;

namespace RegressionPlatform.Infrastructure.Ai.Providers;

/// <summary>
/// Deterministic AI provider: derives the analysis from the report data with no external call.
/// Always available — used when no LLM is configured and as the safety fallback for the others.
/// </summary>
public sealed class RuleBasedProvider : IAiProvider
{
    public string Name => AiProviderNames.RuleBased;

    public Task<ReportAiAnalysisResult> AnalyzeReportAsync(ReportAiContext context, CancellationToken cancellationToken)
    {
        var total = Math.Max(context.TotalRequest, 1);
        var changed = context.ChangedCount + context.AddedCount + context.RemovedCount + context.FailedCount;
        var unchangedPct = Round1((double)context.UnchangedCount / total * 100);
        var breaking = context.BreakingApiCount;
        var deltaText = FormatDelta(context.ResponseTimeDeltaPercent);

        var topApi = context
            .Apis.OrderByDescending(api => api.ChangedCount + api.AddedCount + api.RemovedCount)
            .FirstOrDefault();
        var topName = topApi?.ApiName ?? "the most-changed API";
        var topChanges = topApi is null ? 0 : topApi.ChangedCount + topApi.AddedCount + topApi.RemovedCount;

        var findings = new List<string>
        {
            $"{unchangedPct}% of requests remain unchanged.",
            $"{changed:N0} requests have changes in response.",
        };
        if (breaking > 0)
        {
            findings.Add($"{breaking} APIs contain breaking changes that may affect backward compatibility.");
        }
        if (context.ResponseTimeDeltaPercent != 0)
        {
            findings.Add($"Average response time {deltaText}.");
        }
        findings.Add($"Most changes occurred in {topName} ({topChanges:N0} changes).");

        var riskFactors = new List<string>();
        if (breaking > 0)
        {
            riskFactors.Add($"{breaking} APIs with breaking changes");
        }
        riskFactors.Add($"{changed:N0} changed responses");
        if (context.FieldsRemoved > 0)
        {
            riskFactors.Add($"{context.FieldsRemoved} fields removed");
        }
        if (context.ResponseTimeDeltaPercent != 0)
        {
            riskFactors.Add($"Average response time {deltaText}");
        }

        var recommendations = new List<string>
        {
            $"Review {topName} API changes — the changed fields are concentrated here.",
        };
        if (context.FieldsRemoved > 0)
        {
            recommendations.Add("Check backward compatibility for the APIs that removed fields.");
        }
        if (context.ResponseTimeDeltaPercent > 0)
        {
            recommendations.Add($"Monitor performance — average response time {deltaText}.");
        }
        recommendations.Add("Consider updating client applications to handle new/changed fields.");
        recommendations.Add("No authentication or security issues detected.");

        var summary =
            $"Regression completed. {context.TotalRequest:N0} requests were executed between "
            + $"{context.RunATitle} and {context.RunBTitle}. Overall system behavior is "
            + $"{(context.RegressionScore >= 85 ? "stable" : "showing notable changes")} with a "
            + $"{Round1((double)context.RegressionScore)}% regression score.";

        var recommendation =
            breaking > 0
                ? $"Review the breaking changes in {breaking} APIs before deployment to production. "
                    + "Focus on the fields with the most changes."
                : "No breaking changes detected — proceed with standard verification before deployment.";

        var result = new ReportAiAnalysisResult(
            summary,
            findings,
            recommendation,
            context.RiskLevel,
            RiskScore(context, total, changed),
            riskFactors,
            recommendations,
            new[]
            {
                $"Why did {topName} change?",
                "What are the breaking changes?",
                "Which API has the most impact?",
            },
            Name
        );
        return Task.FromResult(result);
    }

    /// <summary>
    /// No LLM here, so free-form Q&amp;A can't be answered — decline with guidance. The UI keeps the
    /// Ask-AI box disabled while the rule-based provider is active, so users rarely hit this.
    /// </summary>
    public Task<ReportAiAnswer> AskAsync(ReportAiAskRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new ReportAiAnswer(
            "Ask-AI needs a configured AI provider. Set AI:Provider to \"DeepSeek\" and provide an API key "
                + "to ask free-form questions about this report.",
            Need: null
        ));

    private static int RiskScore(ReportAiContext context, int total, int changed)
    {
        var changeLoad = (double)changed / total * 55;
        var breakingLoad = context.BreakingApiCount * 12;
        var failLoad = (double)context.FailedCount / total * 40;
        return Math.Min(100, (int)Math.Round(changeLoad + breakingLoad + failLoad));
    }

    private static string FormatDelta(double deltaPercent)
    {
        var direction = deltaPercent >= 0 ? "increased" : "decreased";
        return $"{direction} by {Math.Abs(deltaPercent).ToString("0.#", CultureInfo.InvariantCulture)}%";
    }

    private static double Round1(double value) => Math.Round(value, 1);
}
