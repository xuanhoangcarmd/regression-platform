using System.Collections.Concurrent;
using RegressionPlatform.Application.Features.Report.Ai;
using RegressionPlatform.Application.Features.Report.Ai.Contracts;

namespace RegressionPlatform.Infrastructure.Ai;

/// <summary>
/// Short-lived in-memory cache of report AI analyses (~15 min), sitting in front of the persisted
/// copy so repeat visits within the window skip the DB read. Entries expire; stale ones are dropped.
/// </summary>
public sealed class ReportAiCache : IReportAiCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<long, Entry> entries = new();

    public ReportAiAnalysisResult? Get(long reportId)
    {
        if (!entries.TryGetValue(reportId, out var entry))
        {
            return null;
        }
        if (entry.ExpiresAt <= DateTime.UtcNow)
        {
            entries.TryRemove(reportId, out _);
            return null;
        }
        return entry.Result;
    }

    public void Set(long reportId, ReportAiAnalysisResult result) =>
        entries[reportId] = new Entry(result, DateTime.UtcNow.Add(Ttl));

    private sealed record Entry(ReportAiAnalysisResult Result, DateTime ExpiresAt);
}
