using RegressionPlatform.Application.Features.Report.Contracts;
using DomainEntities = RegressionPlatform.Domain.Entities;

namespace RegressionPlatform.Application.Features.Report;

/// <summary>Data access for reports (Reports list + detail). Implemented in Infrastructure.</summary>
public interface IReportRepository
{
    /// <summary>
    /// Creates the report, or updates it in place when one already exists for the compare
    /// (keyed by <c>CompareJobId</c>; bumps <c>GeneratedDate</c>). Returns the report id.
    /// </summary>
    Task<long> UpsertAsync(DomainEntities.Report report, CancellationToken cancellationToken);

    /// <summary>All reports for a project, newest first (for the Reports list).</summary>
    Task<IReadOnlyList<ReportListItem>> GetByProjectAsync(int projectId, CancellationToken cancellationToken);

    /// <summary>Full report row (incl. ProjectId + CompareResultId for the detail handler), or null.</summary>
    Task<ReportSummary?> GetByIdAsync(long id, CancellationToken cancellationToken);

    /// <summary>Deletes a report and its stored AI analysis. Leaves the compare/runs untouched.</summary>
    Task DeleteAsync(long id, CancellationToken cancellationToken);

    /// <summary>
    /// Response-time comparison between the report's two runs: per-request durations (paired by
    /// request) and per-API averages, for the Distribution tab charts.
    /// </summary>
    Task<(IReadOnlyList<ReportRequestTiming> PerRequest, IReadOnlyList<ReportApiTiming> PerApi)> GetPerformanceAsync(
        long runAId,
        long runBId,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Per-request raw duration + response size (tagged with API) for both runs — the input for the
    /// Distribution-tab performance statistics (latency histogram + per-API p95/max/size).
    /// </summary>
    Task<IReadOnlyList<ReportPerfRow>> GetPerformanceRowsAsync(long runAId, long runBId, CancellationToken cancellationToken);
}
