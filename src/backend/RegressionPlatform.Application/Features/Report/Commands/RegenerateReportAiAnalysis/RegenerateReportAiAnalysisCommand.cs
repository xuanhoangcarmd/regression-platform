using MediatR;
using RegressionPlatform.Application.Features.Report.Ai.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Report.Commands.RegenerateReportAiAnalysis;

/// <summary>Force a re-run of the AI provider for a report (the "Regenerate" button); persists in place.</summary>
public sealed record RegenerateReportAiAnalysisCommand(string ProjectCode, long ReportId)
    : IRequest<Result<ReportAiAnalysisResult>>;
