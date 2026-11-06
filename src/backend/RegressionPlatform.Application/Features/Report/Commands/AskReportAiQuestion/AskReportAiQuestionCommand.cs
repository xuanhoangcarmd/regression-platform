using MediatR;
using RegressionPlatform.Application.Features.Report.Ai.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Report.Commands.AskReportAiQuestion;

/// <summary>Answer a single user question about a report ("Ask AI about this report"), scoped to its data.</summary>
public sealed record AskReportAiQuestionCommand(string ProjectCode, long ReportId, string Question)
    : IRequest<Result<ReportAiAskResponse>>;
