using RegressionPlatform.Application.Features.Compare.Contracts;
using RegressionPlatform.Application.Features.Runs.Contracts;
using DomainEntities = RegressionPlatform.Domain.Entities;

namespace RegressionPlatform.Application.Features.Compare;

/// <summary>Data access used by the compare engine (background worker). Implemented in Infrastructure.</summary>
public interface ICompareExecutionRepository
{
    /// <summary>Job info needed to execute, or null if the job is gone.</summary>
    Task<CompareExecutionInfo?> GetExecutionInfoAsync(long jobId, CancellationToken cancellationToken);

    /// <summary>API ids the compare targets (from CompareJobApi). Empty = all APIs.</summary>
    Task<IReadOnlyList<int>> GetScopeApiIdsAsync(long jobId, CancellationToken cancellationToken);

    /// <summary>
    /// Enabled compare ignore rules for the project (with the APIs each applies to). Applied to the
    /// stored canonical bodies at compare time so newly-added rules take effect without re-running.
    /// </summary>
    Task<IReadOnlyList<IgnoreRulePath>> GetIgnorePathsAsync(int projectId, CancellationToken cancellationToken);

    /// <summary>
    /// Enabled extract-root rules for the project (with the APIs each applies to). Each lifts the
    /// subtree at its JsonPath to the response root before comparison (envelope unwrapping), applied
    /// to the stored bodies at compare time so rules take effect without re-running the captures.
    /// </summary>
    Task<IReadOnlyList<IgnoreRulePath>> GetExtractRootPathsAsync(int projectId, CancellationToken cancellationToken);

    /// <summary>
    /// Enabled TrimAfter rules for the project (with the APIs each applies to): string values at the
    /// path are compared only up to the delimiter. Applied to the stored bodies at compare time.
    /// </summary>
    Task<IReadOnlyList<TrimRulePath>> GetTrimRulesAsync(int projectId, CancellationToken cancellationToken);

    /// <summary>
    /// Lightweight per-request response info (hash + status + error) for a run, optionally scoped to
    /// <paramref name="apiIds"/> (empty = all of the run's responses). <paramref name="useExpected"/>:
    /// take the edited (expected) response's hash/status when one exists ('Expected' mode).
    /// </summary>
    Task<IReadOnlyList<CompareResponseSummary>> GetResponseSummariesAsync(
        long runId,
        IReadOnlyList<int> apiIds,
        bool useExpected,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Both runs' canonical response bodies for a batch of requests (deep compare), keyed by request
    /// id. <paramref name="useExpected"/>: each run's edited (expected) body when one exists.
    /// </summary>
    Task<IReadOnlyDictionary<long, (string? JsonA, string? JsonB)>> GetResponseBodiesAsync(
        long runAId,
        long runBId,
        IReadOnlyCollection<long> requestIds,
        bool useExpected,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Marks a job Running (sets StartedDate, Progress 0) only if it is still 'Running'. Returns false
    /// when it was Cancelled (via Stop) while queued — the worker should then skip it.
    /// </summary>
    Task<bool> MarkRunningAsync(long jobId, DateTime startedDate, CancellationToken cancellationToken);

    /// <summary>Updates the job's live progress (0–100).</summary>
    Task UpdateProgressAsync(long jobId, int progress, CancellationToken cancellationToken);

    /// <summary>Inserts the result + all details in one transaction; returns the new CompareResult id.</summary>
    Task<long> SaveResultAsync(
        DomainEntities.CompareResult result,
        IReadOnlyList<DomainEntities.CompareDetail> details,
        CancellationToken cancellationToken
    );

    /// <summary>Finishes a job: Completed, links the result, sets FinishedDate + Progress 100.</summary>
    Task CompleteJobAsync(long jobId, long compareResultId, DateTime finishedDate, CancellationToken cancellationToken);

    /// <summary>Marks a job Failed (sets FinishedDate).</summary>
    Task MarkJobFailedAsync(long jobId, DateTime finishedDate, CancellationToken cancellationToken);
}
