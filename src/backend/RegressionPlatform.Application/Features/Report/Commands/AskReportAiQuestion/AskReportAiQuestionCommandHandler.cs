using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Application.Features.Report.Ai;
using RegressionPlatform.Application.Features.Report.Ai.Contracts;
using RegressionPlatform.Application.Features.Report.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Report.Commands.AskReportAiQuestion;

/// <summary>
/// Answers one Ask-AI question about a report. Flow (token-frugal, single-shot with at most one
/// drill-down expansion): cache → build compact base context → provider answers or requests ONE
/// whitelisted source → fetch it → provider answers with that detail. The provider is scoped to the
/// report's data and refuses off-topic questions. Answers are cached (~15 min) so repeats don't
/// re-bill the LLM; nothing is persisted to the database.
/// </summary>
public sealed class AskReportAiQuestionCommandHandler(
    IReportRepository repository,
    IProjectRepository projectRepository,
    ReportAiContextBuilder contextBuilder,
    IAiProviderResolver providerResolver,
    IReportAiAskCache askCache
) : IRequestHandler<AskReportAiQuestionCommand, Result<ReportAiAskResponse>>
{
    private const string NoDetail = "I don't have enough detail in this report to answer that.";
    private const string ProviderError = "Sorry — I couldn't answer that right now. Please try again in a moment.";

    public async Task<Result<ReportAiAskResponse>> Handle(
        AskReportAiQuestionCommand request,
        CancellationToken cancellationToken
    )
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<ReportAiAskResponse>.NotFound(
                new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist.")
            );
        }

        var report = await repository.GetByIdAsync(request.ReportId, cancellationToken);
        if (report is null || report.ProjectId != projectId.Value)
        {
            return Result<ReportAiAskResponse>.NotFound(
                new Error("Report.NotFound", $"Report {request.ReportId} does not exist in the project.")
            );
        }

        var question = request.Question.Trim();

        var cached = askCache.Get(report.Id, question);
        if (cached is not null)
        {
            return Result<ReportAiAskResponse>.Success(cached);
        }

        var context = await contextBuilder.BuildAsync(report, cancellationToken);
        var provider = providerResolver.Resolve();

        try
        {
            var answer = await AnswerAsync(report, context, question, provider, cancellationToken);
            var response = new ReportAiAskResponse(answer, provider.Name);
            askCache.Set(report.Id, question, response);
            return Result<ReportAiAskResponse>.Success(response);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The provider failed (network / bad response). Degrade gracefully — never surface a 500,
            // and don't cache a transient failure.
            return Result<ReportAiAskResponse>.Success(new ReportAiAskResponse(ProviderError, provider.Name));
        }
    }

    /// <summary>Runs the provider with at most one drill-down expansion (cap = 1 extra source).</summary>
    private async Task<string> AnswerAsync(
        ReportSummary report,
        ReportAiContext context,
        string question,
        IAiProvider provider,
        CancellationToken cancellationToken
    )
    {
        var first = await provider.AskAsync(new ReportAiAskRequest(context, question, FetchedDetail: null), cancellationToken);
        if (first.HasAnswer)
        {
            return first.Answer!;
        }

        if (first.Need is not null)
        {
            var detail = await contextBuilder.FetchSourceAsync(report, first.Need.Source, first.Need.Arg, cancellationToken);
            var second = await provider.AskAsync(new ReportAiAskRequest(context, question, detail), cancellationToken);
            return second.HasAnswer ? second.Answer! : NoDetail;
        }

        return NoDetail;
    }
}
