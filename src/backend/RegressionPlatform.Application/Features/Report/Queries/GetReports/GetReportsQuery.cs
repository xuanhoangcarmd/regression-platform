using MediatR;
using RegressionPlatform.Application.Features.Report.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Report.Queries.GetReports;

/// <summary>Lists a project's reports (newest first) for the Reports screen.</summary>
public sealed record GetReportsQuery(string ProjectCode) : IRequest<Result<IReadOnlyList<ReportListItem>>>;
