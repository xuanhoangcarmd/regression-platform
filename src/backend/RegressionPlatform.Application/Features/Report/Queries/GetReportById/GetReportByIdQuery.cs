using MediatR;
using RegressionPlatform.Application.Features.Report.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Report.Queries.GetReportById;

/// <summary>Report detail (Summary tab) — the snapshot header + the compare's per-API breakdown.</summary>
public sealed record GetReportByIdQuery(string ProjectCode, long ReportId) : IRequest<Result<ReportDetailResponse>>;
