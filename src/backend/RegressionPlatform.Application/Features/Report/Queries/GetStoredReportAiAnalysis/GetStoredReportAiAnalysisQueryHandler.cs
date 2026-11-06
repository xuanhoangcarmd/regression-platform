using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Application.Features.Report.Ai;
using RegressionPlatform.Application.Features.Report.Ai.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Report.Queries.GetStoredReportAiAnalysis;

public sealed class GetStoredReportAiAnalysisQueryHandler(
    IReportRepository repository,
    IProjectRepository projectRepository,
    IReportAiRepository aiRepository,
    IReportAiCache cache
) : IRequestHandler<GetStoredReportAiAnalysisQuery, Result<ReportAiAnalysisResult>>
{
    public async Task<Result<ReportAiAnalysisResult>> Handle(
        GetStoredReportAiAnalysisQuery request,
        CancellationToken cancellationToken
    )
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<ReportAiAnalysisResult>.NotFound(
                new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist.")
            );
        }

        var report = await repository.GetByIdAsync(request.ReportId, cancellationToken);
        if (report is null || report.ProjectId != projectId.Value)
        {
            return Result<ReportAiAnalysisResult>.NotFound(
                new Error("Report.NotFound", $"Report {request.ReportId} does not exist in the project.")
            );
        }

        // Read-only: cache then DB. Never generate here.
        var result = cache.Get(report.Id) ?? await aiRepository.GetByReportIdAsync(report.Id, cancellationToken);
        if (result is null)
        {
            return Result<ReportAiAnalysisResult>.NotFound(
                new Error("ReportAiAnalysis.NotFound", "No AI analysis has been generated for this report yet.")
            );
        }

        cache.Set(report.Id, result);
        return Result<ReportAiAnalysisResult>.Success(result);
    }
}
