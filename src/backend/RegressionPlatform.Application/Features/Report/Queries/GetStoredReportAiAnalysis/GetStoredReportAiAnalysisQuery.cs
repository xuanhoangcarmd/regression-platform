using MediatR;
using RegressionPlatform.Application.Features.Report.Ai.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Report.Queries.GetStoredReportAiAnalysis;

/// <summary>
/// Returns the already-stored AI analysis for a report WITHOUT generating one (NotFound if none).
/// Used by exports so they reuse existing data and never trigger a fresh (billable) AI run.
/// </summary>
public sealed record GetStoredReportAiAnalysisQuery(string ProjectCode, long ReportId)
    : IRequest<Result<ReportAiAnalysisResult>>;
