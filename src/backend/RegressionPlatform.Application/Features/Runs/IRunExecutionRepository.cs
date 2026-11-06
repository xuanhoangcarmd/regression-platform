using RegressionPlatform.Application.Features.Runs.Contracts;
using DomainEntities = RegressionPlatform.Domain.Entities;

namespace RegressionPlatform.Application.Features.Runs;

/// <summary>Data access used by the run executor (background worker). Implemented in Infrastructure.</summary>
public interface IRunExecutionRepository
{
    /// <summary>Run info needed to execute (project + environment), or null if the run is gone.</summary>
    Task<RunExecutionInfo?> GetExecutionInfoAsync(long runId, CancellationToken cancellationToken);

    /// <summary>API ids the run targets (from RunApi). Empty list = all project APIs.</summary>
    Task<IReadOnlyList<int>> GetTargetApiIdsAsync(long runId, CancellationToken cancellationToken);

    /// <summary>
    /// Active requests to execute for a dataset. When <paramref name="apiIds"/> is empty, returns all
    /// active requests in the dataset; otherwise only those whose API is in the set.
    /// </summary>
    Task<IReadOnlyList<RunnableRequest>> GetRunnableRequestsAsync(
        int datasetId,
        IReadOnlyList<int> apiIds,
        CancellationToken cancellationToken
    );

    /// <summary>Enabled ignore rules for the project (with the APIs each applies to).</summary>
    Task<IReadOnlyList<IgnoreRulePath>> GetIgnorePathsAsync(int projectId, CancellationToken cancellationToken);

    /// <summary>Persists one captured response.</summary>
    Task InsertResponseAsync(DomainEntities.RunResponse response, CancellationToken cancellationToken);

    /// <summary>One request's definition (method/endpoint/headers/query/body) by id, or null if gone.</summary>
    Task<RunnableRequest?> GetRunnableRequestAsync(long requestId, CancellationToken cancellationToken);

    /// <summary>Replaces a request's stored response in a run (delete old + insert new), atomically.</summary>
    Task ReplaceResponseAsync(DomainEntities.RunResponse response, CancellationToken cancellationToken);

    /// <summary>
    /// Recomputes a run's TotalRequest + Pass/Warning/Failed counts from its stored responses (same
    /// classification the executor uses). Keeps the run row consistent after a single-request re-run.
    /// </summary>
    Task RecalculateRunCountsAsync(long runId, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically moves a Queued run to Running (sets StartTime) right before execution. Returns
    /// false if the run is no longer Queued (e.g. cancelled while waiting) — the caller must skip it.
    /// </summary>
    Task<bool> TryMarkRunningAsync(long runId, DateTime startTime, CancellationToken cancellationToken);

    /// <summary>
    /// Startup recovery: finalizes every run still marked Running as Failed (they were interrupted
    /// by an app restart — the in-memory queue does not survive one). Returns the number recovered.
    /// </summary>
    Task<int> FailInterruptedRunsAsync(DateTime endTime, CancellationToken cancellationToken);

    /// <summary>Startup recovery: ids of Queued runs (never started — safe to re-enqueue), oldest first.</summary>
    Task<IReadOnlyList<long>> GetQueuedRunIdsAsync(CancellationToken cancellationToken);

    /// <summary>Finalizes a run (Completed/Failed) with timing, totals, and execution-health counts.</summary>
    Task CompleteRunAsync(
        long runId,
        string status,
        DateTime endTime,
        long durationMs,
        int totalRequest,
        int passCount,
        int warningCount,
        int failedCount,
        CancellationToken cancellationToken
    );
}
