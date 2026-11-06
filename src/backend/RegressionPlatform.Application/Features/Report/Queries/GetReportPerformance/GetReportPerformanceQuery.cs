using MediatR;
using RegressionPlatform.Application.Features.Report.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Report.Queries.GetReportPerformance;

/// <summary>Response-time comparison (Run A vs Run B) for a report's Distribution tab charts.</summary>
public sealed record GetReportPerformanceQuery(string ProjectCode, long ReportId)
    : IRequest<Result<ReportPerformanceResponse>>;
