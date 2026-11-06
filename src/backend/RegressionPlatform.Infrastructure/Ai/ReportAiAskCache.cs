using System.Collections.Concurrent;
using RegressionPlatform.Application.Features.Report.Ai;
using RegressionPlatform.Application.Features.Report.Ai.Contracts;

namespace RegressionPlatform.Infrastructure.Ai;

/// <summary>
/// Short-lived in-memory cache of Ask-AI answers (~15 min), keyed by report + normalized question,
/// so repeat questions (e.g. clicking the same example prompt) don't re-bill the LLM. A report is an
/// immutable snapshot, so a cached answer stays valid for the window. Entries expire; stale ones are
/// dropped on read. To bound memory, oldest entries are trimmed once the map grows large.
/// </summary>
public sealed class ReportAiAskCache : IReportAiAskCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(15);
    private const int MaxEntries = 500;

    private readonly ConcurrentDictionary<string, Entry> entries = new();

    public ReportAiAskResponse? Get(long reportId, string question)
    {
        var key = Key(reportId, question);
        if (!entries.TryGetValue(key, out var entry))
        {
            return null;
        }
        if (entry.ExpiresAt <= DateTime.UtcNow)
        {
            entries.TryRemove(key, out _);
            return null;
        }
        return entry.Response;
    }

    public void Set(long reportId, string question, ReportAiAskResponse response)
    {
        if (entries.Count >= MaxEntries)
        {
            TrimExpired();
        }
        entries[Key(reportId, question)] = new Entry(response, DateTime.UtcNow.Add(Ttl));
    }

    private void TrimExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var pair in entries)
        {
            if (pair.Value.ExpiresAt <= now)
            {
                entries.TryRemove(pair.Key, out _);
            }
        }
    }

    private static string Key(long reportId, string question) =>
        $"{reportId}::{question.Trim().ToLowerInvariant()}";

    private sealed record Entry(ReportAiAskResponse Response, DateTime ExpiresAt);
}
