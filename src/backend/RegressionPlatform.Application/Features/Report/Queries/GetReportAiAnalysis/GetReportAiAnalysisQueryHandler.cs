using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Application.Features.Report.Ai;
using RegressionPlatform.Application.Features.Report.Ai.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Report.Queries.GetReportAiAnalysis;

public sealed class GetReportAiAnalysisQueryHandler(
    IReportRepository repository,
    IProjectRepository projectRepository,
    IReportAiRepository aiRepository,
    IReportAiCache cache,
    ReportAiRunner runner
) : IRequestHandler<GetReportAiAnalysisQuery, Result<ReportAiAnalysisResult>>
{
    public async Task<Result<ReportAiAnalysisResult>> Handle(
        GetReportAiAnalysisQuery request,
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

        // Fast path: short-lived in-memory cache, then the persisted analysis.
        var cached = cache.Get(report.Id);
        if (cached is not null)
        {
            return Result<ReportAiAnalysisResult>.Success(cached);
        }

        var stored = await aiRepository.GetByReportIdAsync(report.Id, cancellationToken);
        if (stored is not null)
        {
            cache.Set(report.Id, stored);
            return Result<ReportAiAnalysisResult>.Success(stored);
        }

        // Nothing stored yet — generate once, persist, cache.
        var result = await runner.RunAsync(report, cancellationToken);
        var generatedAt = await aiRepository.UpsertAsync(report.Id, result, cancellationToken);
        result = result with { GeneratedAt = generatedAt };
        cache.Set(report.Id, result);
        return Result<ReportAiAnalysisResult>.Success(result);
    }
}
