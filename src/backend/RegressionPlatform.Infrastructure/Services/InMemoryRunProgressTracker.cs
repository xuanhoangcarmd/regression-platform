using System.Collections.Concurrent;
using RegressionPlatform.Application.Features.Runs;

namespace RegressionPlatform.Infrastructure.Services;

/// <summary>
/// In-memory progress for currently-executing runs (a singleton, not persisted). Thread-safe:
/// the executor increments from the worker thread while the API reads from request threads.
/// </summary>
public sealed class InMemoryRunProgressTracker : IRunProgressTracker
{
    private sealed class Counter
    {
        public int Total;
        public int Executed;
    }

    private readonly ConcurrentDictionary<long, Counter> _runs = new();

    public void Start(long runId, int total) => _runs[runId] = new Counter { Total = total, Executed = 0 };

    public void Increment(long runId)
    {
        if (_runs.TryGetValue(runId, out var counter))
        {
            Interlocked.Increment(ref counter.Executed);
        }
    }

    public void Remove(long runId) => _runs.TryRemove(runId, out _);

    public RunProgress? Get(long runId) =>
        _runs.TryGetValue(runId, out var counter) ? new RunProgress(Volatile.Read(ref counter.Executed), counter.Total) : null;
}
