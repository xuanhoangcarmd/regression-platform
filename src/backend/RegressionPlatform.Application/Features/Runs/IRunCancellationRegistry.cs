namespace RegressionPlatform.Application.Features.Runs;

/// <summary>
/// In-memory registry of cancellation tokens for currently-executing runs, so a Stop request can
/// signal the background worker to abort a specific run. Entries live only while a run executes;
/// after an app restart the registry is empty (a still-"Running" row is then stopped via DB status).
/// </summary>
public interface IRunCancellationRegistry
{
    /// <summary>
    /// Registers a run for cancellation and returns a token that trips when either the caller's
    /// <paramref name="linkedTo"/> token (app shutdown) or a <see cref="Cancel"/> request fires.
    /// </summary>
    CancellationToken Register(long runId, CancellationToken linkedTo);

    /// <summary>Requests cancellation of a running run. Returns false if it isn't being tracked.</summary>
    bool Cancel(long runId);

    /// <summary>Stops tracking a run (finished/cancelled) and frees its token source.</summary>
    void Remove(long runId);
}
