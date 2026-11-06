using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Report.Commands.DeleteReport;

/// <summary>Deletes a report and all data derived from it (its stored AI analysis). The underlying
/// compare/runs are upstream and are NOT touched.</summary>
public sealed record DeleteReportCommand(string ProjectCode, long ReportId) : IRequest<Result>;
