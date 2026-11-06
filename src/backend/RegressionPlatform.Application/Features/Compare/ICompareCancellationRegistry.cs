namespace RegressionPlatform.Application.Features.Compare;

/// <summary>
/// In-memory registry of cancellation tokens for currently-executing compares, so a Stop request can
/// signal the background worker to abort a specific job. Entries live only while a compare executes;
/// after an app restart the registry is empty (a still-"Running" row is then stopped via DB status).
/// </summary>
public interface ICompareCancellationRegistry
{
    /// <summary>
    /// Registers a compare for cancellation and returns a token that trips when either the caller's
    /// <paramref name="linkedTo"/> token (app shutdown) or a <see cref="Cancel"/> request fires.
    /// </summary>
    CancellationToken Register(long jobId, CancellationToken linkedTo);

    /// <summary>Requests cancellation of a running compare. Returns false if it isn't being tracked.</summary>
    bool Cancel(long jobId);

    /// <summary>Stops tracking a compare (finished/cancelled) and frees its token source.</summary>
    void Remove(long jobId);
}
