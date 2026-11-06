using MediatR;
using RegressionPlatform.Application.Features.Report.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Report.Queries.GetReportPerformanceStats;

/// <summary>Extra performance statistics for a report's Distribution tab (latency histogram + per-API stats).</summary>
public sealed record GetReportPerformanceStatsQuery(string ProjectCode, long ReportId)
    : IRequest<Result<ReportPerformanceStatsResponse>>;
