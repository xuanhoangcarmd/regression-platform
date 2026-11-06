namespace RegressionPlatform.Application.Features.Runs;

/// <summary>Live progress of a running run: how many requests executed out of the total.</summary>
public readonly record struct RunProgress(int Executed, int Total);

/// <summary>
/// In-memory (not persisted) progress of currently-executing runs. The executor reports progress
/// here; the runs query reads it; the entry is removed when the run finishes.
/// </summary>
public interface IRunProgressTracker
{
    /// <summary>Begins tracking a run with its planned total request count.</summary>
    void Start(long runId, int total);

    /// <summary>Marks one more request done.</summary>
    void Increment(long runId);

    /// <summary>Stops tracking (run finished) — frees the memory.</summary>
    void Remove(long runId);

    /// <summary>Current progress, or null if the run is not being tracked.</summary>
    RunProgress? Get(long runId);
}
