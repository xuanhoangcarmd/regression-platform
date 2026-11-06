using RegressionPlatform.Application.Features.Compare.Contracts;
using RegressionPlatform.Shared.Pagination;
using DomainEntities = RegressionPlatform.Domain.Entities;

namespace RegressionPlatform.Application.Features.Compare;

/// <summary>Query-side data access for compares (History + detail screens). Implemented in Infrastructure.</summary>
public interface ICompareRepository
{
    /// <summary>Creates a compare job (Pending) and its API scope links (empty = all APIs). Returns the new id.</summary>
    Task<long> CreateJobAsync(DomainEntities.CompareJob job, IReadOnlyList<int> apiIds, CancellationToken cancellationToken);

    /// <summary>All compare jobs for a project, newest first (for the History list).</summary>
    Task<IReadOnlyList<CompareJobListItem>> GetJobsByProjectAsync(int projectId, CancellationToken cancellationToken);

    /// <summary>Loads the job entity (for ownership/status checks), or null if not found.</summary>
    Task<DomainEntities.CompareJob?> GetJobAsync(long jobId, CancellationToken cancellationToken);

    /// <summary>Running → Cancelled (guarded). Returns true only if the job was Running.</summary>
    Task<bool> MarkCancelledAsync(long jobId, DateTime finishedDate, CancellationToken cancellationToken);

    /// <summary>1 if a report was generated from this compare (blocks deletion); else 0.</summary>
    Task<bool> HasReportAsync(long jobId, CancellationToken cancellationToken);

    /// <summary>Deletes a compare job and all its data (result, per-request details, API links).</summary>
    Task DeleteAsync(long jobId, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically moves a Pending job to Running (sets StartedDate). Returns true only if the job was
    /// Pending — guards against double-start.
    /// </summary>
    Task<bool> TryStartAsync(long jobId, DateTime startedDate, CancellationToken cancellationToken);

    /// <summary>Detail header (job + result summary + run labels), or null if not found.</summary>
    Task<CompareSummaryResponse?> GetSummaryAsync(long jobId, CancellationToken cancellationToken);

    /// <summary>Per-API breakdown for a finished compare result.</summary>
    Task<IReadOnlyList<CompareApiStatistic>> GetApiStatisticsAsync(long compareResultId, CancellationToken cancellationToken);

    /// <summary>All non-empty per-request field diffs for a result (for the change summary).</summary>
    Task<IReadOnlyList<CompareRequestDiff>> GetDiffsAsync(long compareResultId, CancellationToken cancellationToken);

    /// <summary>Paged per-request detail list, with search + result filter.</summary>
    Task<PagedResult<CompareDetailListItem>> GetDetailsPagedAsync(
        CompareDetailListParameters parameters,
        CancellationToken cancellationToken
    );

    /// <summary>All per-request details (identity + result + raw field diff) for a result — for the full export.</summary>
    Task<IReadOnlyList<CompareFullDetailItem>> GetFullDetailsAsync(long compareResultId, CancellationToken cancellationToken);

    /// <summary>Every request of a finished compare with both runs' stored responses (Test Results export).</summary>
    Task<IReadOnlyList<CompareTestResultItem>> GetTestResultsAsync(
        long compareResultId,
        long runAId,
        long runBId,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Full diff for one request (field diff + both runs' bodies), or null if not found. The returned
    /// bodies have the project's current ignore rules applied (scoped to the request's API), so the
    /// side-by-side view matches the ignore-aware field diff.
    /// </summary>
    Task<CompareDetailDiffResponse?> GetDetailDiffAsync(
        int projectId,
        long compareResultId,
        long runAId,
        long runBId,
        long requestId,
        bool ignoreOrder,
        CancellationToken cancellationToken
    );
}
