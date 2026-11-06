using System.Globalization;
using System.Text;
using RegressionPlatform.Application.Features.Compare;
using RegressionPlatform.Application.Features.Report.Ai.Contracts;
using RegressionPlatform.Application.Features.Report.Contracts;

namespace RegressionPlatform.Application.Features.Report.Ai;

/// <summary>
/// Builds the AI grounding data for a report. Two shapes:
/// <list type="bullet">
/// <item><see cref="BuildAsync"/> — the compact base <see cref="ReportAiContext"/> (headline counts,
/// per-API breakdown, top change patterns, response-time delta) sent up-front for both the summary
/// and Ask-AI.</item>
/// <item><see cref="FetchSourceAsync"/> — one whitelisted, truncated drill-down source, fetched only
/// when an Ask-AI provider explicitly requests it (token-frugal on-demand expansion).</item>
/// </list>
/// Pure orchestration over the repositories — no external calls.
/// </summary>
public sealed class ReportAiContextBuilder(
    IReportRepository reportRepository,
    ICompareRepository compareRepository
)
{
    private const string FieldRemoved = "FieldRemoved";
    private const string TypeChanged = "TypeChanged";
    private const int MaxPatterns = 40;

    // Drill-down limits — keep fetched detail small (the whole point of on-demand retrieval).
    private const int MaxDetailPatterns = 12;
    private const int MaxSamplesPerPattern = 2;
    private const int MaxSampleLength = 80;
    private const int MaxSlowestRequests = 15;

    /// <summary>Builds the compact base context for a report (shared by summary + Ask-AI).</summary>
    public async Task<ReportAiContext> BuildAsync(ReportSummary report, CancellationToken cancellationToken)
    {
        var apiStats = await compareRepository.GetApiStatisticsAsync(report.CompareResultId, cancellationToken);
        var diffs = await compareRepository.GetDiffsAsync(report.CompareResultId, cancellationToken);
        var (perRequest, _) = await reportRepository.GetPerformanceAsync(report.RunAId, report.RunBId, cancellationToken);

        var changeGroups = ChangeSummaryAggregator.Aggregate(diffs, maxSamples: 0);

        var breakingByApi = changeGroups.ToDictionary(
            group => group.ApiId,
            group => group.Patterns.Count(pattern => pattern.ChangeType is FieldRemoved or TypeChanged)
        );

        var apis = apiStats
            .Select(api => new ReportAiApiContext(
                api.ApiName,
                api.Method,
                api.Endpoint,
                api.RequestCount,
                api.ChangedCount,
                api.AddedCount,
                api.RemovedCount,
                api.FailedCount,
                breakingByApi.GetValueOrDefault(api.ApiId, 0)
            ))
            .ToList();

        var patterns = changeGroups
            .SelectMany(group => group.Patterns.Select(pattern =>
                new ReportAiChangePattern(group.ApiName ?? string.Empty, pattern.ChangeType, pattern.Path, pattern.Occurrences)
            ))
            .OrderByDescending(pattern => pattern.Occurrences)
            .Take(MaxPatterns)
            .ToList();

        var fieldsRemoved = changeGroups
            .SelectMany(group => group.Patterns)
            .Count(pattern => pattern.ChangeType == FieldRemoved);

        var responseTimeDelta = ComputeResponseTimeDelta(perRequest);

        return new ReportAiContext(
            report.Name,
            report.RunATitle,
            report.RunBTitle,
            report.RegressionScore,
            report.RiskLevel,
            report.TotalRequest,
            report.UnchangedCount,
            report.ChangedCount,
            report.AddedCount,
            report.RemovedCount,
            report.FailedCount,
            report.TotalApis,
            report.BreakingApiCount,
            fieldsRemoved,
            responseTimeDelta,
            apis,
            patterns
        );
    }

    /// <summary>
    /// Fetches one whitelisted drill-down source as compact, truncated text. Unknown sources return a
    /// short note (never throws) so the provider can still answer with what it has.
    /// </summary>
    public async Task<string> FetchSourceAsync(
        ReportSummary report,
        string source,
        string? arg,
        CancellationToken cancellationToken
    ) => source switch
    {
        ReportAiSources.ApiDetail => await BuildApiDetailAsync(report, arg, cancellationToken),
        ReportAiSources.SlowestRequests => await BuildSlowestRequestsAsync(report, cancellationToken),
        _ => $"Unknown data source '{source}'. Answer using the data already provided.",
    };

    /// <summary>Field-level change patterns (with old→new samples) for one API, truncated for tokens.</summary>
    private async Task<string> BuildApiDetailAsync(ReportSummary report, string? apiName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiName))
        {
            return "No API name was provided. Ask about a specific API listed in the data.";
        }

        var diffs = await compareRepository.GetDiffsAsync(report.CompareResultId, cancellationToken);
        var groups = ChangeSummaryAggregator.Aggregate(diffs, maxSamples: MaxSamplesPerPattern);

        var group = groups.FirstOrDefault(g =>
            string.Equals(g.ApiName, apiName, StringComparison.OrdinalIgnoreCase));
        if (group is null)
        {
            return $"No changes recorded for an API named '{apiName}' in this report.";
        }

        var builder = new StringBuilder();
        builder.Append(CultureInfo.InvariantCulture, $"API \"{group.ApiName}\" ({group.Method} {group.Endpoint}) — ");
        builder.Append(CultureInfo.InvariantCulture, $"{group.PatternCount} distinct change pattern(s):");

        foreach (var pattern in group.Patterns.Take(MaxDetailPatterns))
        {
            builder.Append(CultureInfo.InvariantCulture,
                $"\n- {pattern.ChangeType} {pattern.Path} ×{pattern.Occurrences} ({pattern.RequestCount} request(s))");
            var samples = pattern.Samples
                .Take(MaxSamplesPerPattern)
                .Select(sample => $"{Short(sample.Old)}→{Short(sample.New)}")
                .ToList();
            if (samples.Count > 0)
            {
                builder.Append(CultureInfo.InvariantCulture, $"; e.g. {string.Join("; ", samples)}");
            }
        }

        if (group.PatternCount > MaxDetailPatterns)
        {
            builder.Append(CultureInfo.InvariantCulture, $"\n… (+{group.PatternCount - MaxDetailPatterns} more pattern(s))");
        }

        return builder.ToString();
    }

    /// <summary>Top requests by Run B response time, with Run A → Run B durations and delta.</summary>
    private async Task<string> BuildSlowestRequestsAsync(ReportSummary report, CancellationToken cancellationToken)
    {
        var (perRequest, _) = await reportRepository.GetPerformanceAsync(report.RunAId, report.RunBId, cancellationToken);

        var slowest = perRequest
            .Where(row => row.DurationMsB.HasValue)
            .OrderByDescending(row => row.DurationMsB!.Value)
            .Take(MaxSlowestRequests)
            .ToList();

        if (slowest.Count == 0)
        {
            return "No response-time measurements are available for the current run.";
        }

        var builder = new StringBuilder("Requests with the highest Run B response time (Run A → Run B):");
        foreach (var row in slowest)
        {
            var a = row.DurationMsA;
            var b = row.DurationMsB!.Value;
            var delta = a is > 0 ? $" ({FormatDelta((double)(b - a.Value) / a.Value * 100)})" : string.Empty;
            builder.Append(CultureInfo.InvariantCulture,
                $"\n- {row.Title} [{row.Method} {row.Endpoint}]: {(a.HasValue ? a + "ms" : "n/a")} → {b}ms{delta}");
        }

        return builder.ToString();
    }

    /// <summary>Percentage change in average response time (Run A → Run B); 0 if not measurable.</summary>
    private static double ComputeResponseTimeDelta(IReadOnlyList<ReportRequestTiming> perRequest)
    {
        var avgA = perRequest.Where(row => row.DurationMsA.HasValue).Select(row => (double)row.DurationMsA!.Value).DefaultIfEmpty().Average();
        var avgB = perRequest.Where(row => row.DurationMsB.HasValue).Select(row => (double)row.DurationMsB!.Value).DefaultIfEmpty().Average();
        if (avgA <= 0)
        {
            return 0;
        }
        return Math.Round((avgB - avgA) / avgA * 100, 1);
    }

    private static string FormatDelta(double deltaPercent)
    {
        var sign = deltaPercent >= 0 ? "+" : "-";
        return $"{sign}{Math.Abs(deltaPercent).ToString("0.#", CultureInfo.InvariantCulture)}%";
    }

    /// <summary>Compact display for a sample value: null → ∅, else truncated.</summary>
    private static string Short(string? value)
    {
        if (value is null)
        {
            return "∅";
        }
        return value.Length <= MaxSampleLength ? value : value[..MaxSampleLength] + "…";
    }
}
