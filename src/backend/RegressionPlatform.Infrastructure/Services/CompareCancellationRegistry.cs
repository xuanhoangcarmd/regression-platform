using System.Collections.Concurrent;
using RegressionPlatform.Application.Features.Compare;

namespace RegressionPlatform.Infrastructure.Services;

/// <summary>
/// In-memory registry of per-compare cancellation sources (a singleton). The executor registers a
/// job when it starts and removes it when done; a Stop request cancels the matching token so the
/// executor's loop aborts. Thread-safe (worker registers/removes; request threads cancel).
/// </summary>
public sealed class CompareCancellationRegistry : ICompareCancellationRegistry
{
    private readonly ConcurrentDictionary<long, CancellationTokenSource> _jobs = new();

    public CancellationToken Register(long jobId, CancellationToken linkedTo)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(linkedTo);
        // If a stale entry exists (shouldn't, but be safe), dispose it before replacing.
        if (_jobs.TryRemove(jobId, out var existing))
        {
            existing.Dispose();
        }
        _jobs[jobId] = cts;
        return cts.Token;
    }

    public bool Cancel(long jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var cts))
        {
            return false;
        }
        cts.Cancel();
        return true;
    }

    public void Remove(long jobId)
    {
        if (_jobs.TryRemove(jobId, out var cts))
        {
            cts.Dispose();
        }
    }
}
