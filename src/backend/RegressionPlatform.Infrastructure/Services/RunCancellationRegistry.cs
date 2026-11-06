using System.Collections.Concurrent;
using RegressionPlatform.Application.Features.Runs;

namespace RegressionPlatform.Infrastructure.Services;

/// <summary>
/// In-memory registry of per-run cancellation sources (a singleton). The executor registers a run
/// when it starts and removes it when done; a Stop request cancels the matching token so the
/// executor's loop aborts. Thread-safe (worker registers/removes; request threads cancel).
/// </summary>
public sealed class RunCancellationRegistry : IRunCancellationRegistry
{
    private readonly ConcurrentDictionary<long, CancellationTokenSource> _runs = new();

    public CancellationToken Register(long runId, CancellationToken linkedTo)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(linkedTo);
        // If a stale entry exists (shouldn't, but be safe), dispose it before replacing.
        if (_runs.TryRemove(runId, out var existing))
        {
            existing.Dispose();
        }
        _runs[runId] = cts;
        return cts.Token;
    }

    public bool Cancel(long runId)
    {
        if (!_runs.TryGetValue(runId, out var cts))
        {
            return false;
        }
        cts.Cancel();
        return true;
    }

    public void Remove(long runId)
    {
        if (_runs.TryRemove(runId, out var cts))
        {
            cts.Dispose();
        }
    }
}
