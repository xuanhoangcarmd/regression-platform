using MediatR;
using RegressionPlatform.Application.Features.Report.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Report.Commands.GenerateReport;

/// <summary>
/// Generates (or regenerates, in place) the report for a Completed compare. One report per
/// compare — regenerating refreshes the snapshot and bumps GeneratedDate.
/// </summary>
public sealed record GenerateReportCommand(string ProjectCode, long CompareId) : IRequest<Result<ReportListItem>>;
