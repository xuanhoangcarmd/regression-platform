using MediatR;
using RegressionPlatform.Application.Features.Report.Ai.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Report.Queries.GetReportAiAnalysis;

/// <summary>AI analysis of a report (runs the configured AI provider) for the AI Analysis tab.</summary>
public sealed record GetReportAiAnalysisQuery(string ProjectCode, long ReportId)
    : IRequest<Result<ReportAiAnalysisResult>>;
