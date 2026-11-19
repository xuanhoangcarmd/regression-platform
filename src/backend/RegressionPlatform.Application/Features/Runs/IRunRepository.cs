using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Shared.Pagination;
using DomainEntities = RegressionPlatform.Domain.Entities;

namespace RegressionPlatform.Application.Features.Runs;

/// <summary>Data access for regression runs. Implemented in Infrastructure.</summary>
public interface IRunRepository
{
    /// <summary>All runs for a project, newest first.</summary>
    Task<IReadOnlyList<RunResponse>> GetByProjectAsync(int projectId, CancellationToken cancellationToken);

    /// <summary>
    /// Lightweight progress snapshot for specific runs (the live-progress poll target). Scoped to the
    /// project; ids not belonging to it are simply absent from the result.
    /// </summary>
    Task<IReadOnlyList<RunProgressResponse>> GetProgressByIdsAsync(
        int projectId,
        IReadOnlyList<long> runIds,
        CancellationToken cancellationToken
    );

    Task<RunResponse?> GetByIdAsync(long id, CancellationToken cancellationToken);

    /// <summary>Loads the run entity (for status/ownership checks + detail), or null if not found.</summary>
    Task<DomainEntities.Run?> GetEntityAsync(long id, CancellationToken cancellationToken);

    /// <summary>Per-API execution statistics for a run (for the detail Summary tab).</summary>
    Task<IReadOnlyList<ApiRunStatistic>> GetApiStatisticsAsync(long runId, CancellationToken cancellationToken);

    /// <summary>Paged list of a run's executed requests (Requests tab), with search + status filter.</summary>
    Task<PagedResult<RunResponseListItem>> GetResponsesPagedAsync(
        RunResponseListParameters parameters,
        CancellationToken cancellationToken
    );

    /// <summary>Full detail of one executed request in a run (Results tab), or null if not found.</summary>
    Task<RunResponseDetail?> GetResponseDetailAsync(long runId, long requestId, CancellationToken cancellationToken);

    /// <summary>Every executed request of a run (input + full response body) — for the Excel export.</summary>
    Task<IReadOnlyList<RunResponseExportRow>> GetResponsesForExportAsync(long runId, CancellationToken cancellationToken);

    /// <summary>Creates a run and its API links (empty <paramref name="apiIds"/> = all APIs). Returns the new id.</summary>
    Task<long> CreateAsync(DomainEntities.Run run, IReadOnlyList<int> apiIds, CancellationToken cancellationToken);

    /// <summary>Target API ids for a run (from RunApi); empty list = the run targets all of the dataset's APIs.</summary>
    Task<IReadOnlyList<int>> GetApiIdsAsync(long runId, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an editable (Pending) run's fields and replaces its API links, in one transaction.
    /// Returns true only if the run was Pending (guards editing a started run).
    /// </summary>
    Task<bool> UpdateAsync(DomainEntities.Run run, IReadOnlyList<int> apiIds, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically moves a Pending run to Queued (awaiting the background worker). Returns true only
    /// if the run was Pending — guards against double-start. The worker sets Running + StartTime
    /// when execution actually begins.
    /// </summary>
    Task<bool> MarkQueuedAsync(long id, CancellationToken cancellationToken);

    /// <summary>
    /// Marks a Queued or Running run as Cancelled (sets EndTime). Returns true only if it was
    /// Queued/Running — so a run that already finished/failed isn't retroactively changed.
    /// </summary>
    Task<bool> MarkCancelledAsync(long id, DateTime endTime, CancellationToken cancellationToken);

    /// <summary>
    /// Re-run reset, in one transaction: moves a finished run (Completed / Failed / Cancelled) back
    /// to Queued, clears its timing + counts and deletes its captured responses (expected-response
    /// overrides are kept). Returns false if the run was not finished (nothing is changed).
    /// </summary>
    Task<bool> ResetForRerunAsync(long id, CancellationToken cancellationToken);

    /// <summary>True if this run is referenced (as baseline/current) by any compare job or result.</summary>
    Task<bool> HasCompareAsync(long id, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a run and its executed responses + API links, in one transaction. Only valid for a run
    /// with no compare (callers must check <see cref="HasCompareAsync"/> first).
    /// </summary>
    Task DeleteAsync(long id, CancellationToken cancellationToken);
}
