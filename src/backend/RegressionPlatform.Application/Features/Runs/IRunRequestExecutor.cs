namespace RegressionPlatform.Application.Features.Runs;

/// <summary>Re-executes a single request inside an existing run and updates its stored result.</summary>
public interface IRunRequestExecutor
{
    /// <summary>
    /// Re-runs one request of a run: calls the API again (same environment config + ignore rules),
    /// replaces that request's stored response, and recomputes the run's health counts. Returns false
    /// when it can't run — the run/request is gone, or the environment has no Base URL configured.
    /// </summary>
    Task<bool> RerunRequestAsync(long runId, long requestId, CancellationToken cancellationToken);
}
