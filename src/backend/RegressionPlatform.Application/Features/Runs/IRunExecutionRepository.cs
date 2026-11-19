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

    /// <summary>
    /// Persists one captured response — only while this instance still owns the Running run, and never
    /// twice for the same request (see <see cref="ResponseInsertResult"/>).
    /// </summary>
    Task<ResponseInsertResult> InsertResponseAsync(DomainEntities.RunResponse response, CancellationToken cancellationToken);

    /// <summary>
    /// Owner heartbeat for a Running run. False = this instance no longer owns it (stopped from any
    /// instance, re-run, or failed by recovery) and the executor must stop.
    /// </summary>
    Task<bool> HeartbeatAsync(long runId, CancellationToken cancellationToken);

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
    /// Atomically moves a Queued run to Running (sets StartTime) right before execution and claims it
    /// for this instance. Returns false if the run is no longer Queued (e.g. cancelled while waiting,
    /// or claimed by another instance) — the caller must skip it.
    /// </summary>
    Task<bool> TryMarkRunningAsync(long runId, DateTime startTime, CancellationToken cancellationToken);

    /// <summary>
    /// Recovery: finalizes as Failed every Running run whose owner has not sent a heartbeat for
    /// <paramref name="staleAfter"/> (process killed / restarted). Runs a live instance — here or on
    /// another machine sharing the DB — is executing are left alone. Returns the number recovered.
    /// </summary>
    Task<int> FailStaleRunsAsync(DateTime endTime, TimeSpan staleAfter, CancellationToken cancellationToken);

    /// <summary>
    /// Startup recovery: ids of Queued runs queued from this node (never started — safe to re-enqueue),
    /// oldest first. Runs queued by another instance sharing the DB are left to that instance.
    /// </summary>
    Task<IReadOnlyList<long>> GetQueuedRunIdsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Finalizes a run (Completed/Failed) with timing, totals, and execution-health counts — only if it
    /// is still Running under this instance (a lost run is never overwritten).
    /// </summary>
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
