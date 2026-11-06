using MediatR;
using RegressionPlatform.Application.Features.Compare;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Application.Features.Report.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Report.Queries.GetReportById;

public sealed class GetReportByIdQueryHandler(
    IReportRepository repository,
    ICompareRepository compareRepository,
    IProjectRepository projectRepository
) : IRequestHandler<GetReportByIdQuery, Result<ReportDetailResponse>>
{
    private const string FieldRemoved = "FieldRemoved";
    private const string TypeChanged = "TypeChanged";

    public async Task<Result<ReportDetailResponse>> Handle(
        GetReportByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<ReportDetailResponse>.NotFound(
                new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist.")
            );
        }

        var report = await repository.GetByIdAsync(request.ReportId, cancellationToken);
        if (report is null || report.ProjectId != projectId.Value)
        {
            return Result<ReportDetailResponse>.NotFound(
                new Error("Report.NotFound", $"Report {request.ReportId} does not exist in the project.")
            );
        }

        // The per-API breakdown is read live from the (immutable) compare result the report snapshotted.
        var apiStats = await compareRepository.GetApiStatisticsAsync(report.CompareResultId, cancellationToken);

        // Count backward-incompatible (breaking) change patterns per API from the raw diffs.
        var diffs = await compareRepository.GetDiffsAsync(report.CompareResultId, cancellationToken);
        var breakingByApi = ChangeSummaryAggregator
            .Aggregate(diffs, maxSamples: 1)
            .ToDictionary(
                group => group.ApiId,
                group => group.Patterns.Count(pattern => pattern.ChangeType is FieldRemoved or TypeChanged)
            );

        var apis = apiStats
            .Select(api => new ReportApiStat(
                api.ApiId,
                api.ApiName,
                api.Method,
                api.Endpoint,
                api.RequestCount,
                api.UnchangedCount,
                api.ChangedCount,
                api.AddedCount,
                api.RemovedCount,
                api.FailedCount,
                breakingByApi.GetValueOrDefault(api.ApiId, 0)
            ))
            .ToList();

        var response = new ReportDetailResponse(
            report.Id,
            report.CompareJobId,
            report.RunAId,
            report.RunBId,
            report.RunATitle,
            report.RunBTitle,
            report.Name,
            report.Status,
            report.RegressionScore,
            report.RiskLevel,
            report.TotalRequest,
            report.UnchangedCount,
            report.ChangedCount,
            report.AddedCount,
            report.RemovedCount,
            report.FailedCount,
            report.TotalApis,
            report.BreakingApiCount,
            report.GeneratedDate,
            report.CreatedDate,
            apis
        );
        return Result<ReportDetailResponse>.Success(response);
    }
}
