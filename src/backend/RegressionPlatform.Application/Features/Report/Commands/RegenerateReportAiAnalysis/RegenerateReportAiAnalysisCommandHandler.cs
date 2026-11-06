using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Application.Features.Report;
using RegressionPlatform.Application.Features.Report.Ai;
using RegressionPlatform.Application.Features.Report.Ai.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Report.Commands.RegenerateReportAiAnalysis;

public sealed class RegenerateReportAiAnalysisCommandHandler(
    IReportRepository repository,
    IProjectRepository projectRepository,
    IReportAiRepository aiRepository,
    IReportAiCache cache,
    ReportAiRunner runner
) : IRequestHandler<RegenerateReportAiAnalysisCommand, Result<ReportAiAnalysisResult>>
{
    public async Task<Result<ReportAiAnalysisResult>> Handle(
        RegenerateReportAiAnalysisCommand request,
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

        // Force a fresh run, persist in place and refresh the cache.
        var result = await runner.RunAsync(report, cancellationToken);
        var generatedAt = await aiRepository.UpsertAsync(report.Id, result, cancellationToken);
        result = result with { GeneratedAt = generatedAt };
        cache.Set(report.Id, result);
        return Result<ReportAiAnalysisResult>.Success(result);
    }
}
