using MediatR;
using RegressionPlatform.Application.Features.Compare;
using RegressionPlatform.Application.Features.Compare.Contracts;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Application.Features.Report.Contracts;
using RegressionPlatform.Shared.Results;
using DomainEntities = RegressionPlatform.Domain.Entities;

namespace RegressionPlatform.Application.Features.Report.Commands.GenerateReport;

public sealed class GenerateReportCommandHandler(
    IReportRepository reportRepository,
    ICompareRepository compareRepository,
    IProjectRepository projectRepository
) : IRequestHandler<GenerateReportCommand, Result<ReportListItem>>
{
    // Change types that count as backward-incompatible ("breaking") for the risk heuristic.
    private const string FieldRemoved = "FieldRemoved";
    private const string TypeChanged = "TypeChanged";

    public async Task<Result<ReportListItem>> Handle(GenerateReportCommand request, CancellationToken cancellationToken)
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<ReportListItem>.NotFound(
                new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist.")
            );
        }

        var compare = await compareRepository.GetSummaryAsync(request.CompareId, cancellationToken);
        if (compare is null || compare.ProjectId != projectId.Value)
        {
            return Result<ReportListItem>.NotFound(
                new Error("Compare.NotFound", $"Compare {request.CompareId} does not exist in the project.")
            );
        }

        if (compare.Status != CompareStatuses.Completed || compare.CompareResultId is not long resultId)
        {
            return Result<ReportListItem>.Failure(
                new Error("Report.CompareNotCompleted", "The compare must be Completed before a report can be generated.")
            );
        }

        // Per-API breakdown (Total APIs) and distinct change patterns per API (breaking-change count).
        var apiStats = await compareRepository.GetApiStatisticsAsync(resultId, cancellationToken);
        var diffs = await compareRepository.GetDiffsAsync(resultId, cancellationToken);
        var apiGroups = ChangeSummaryAggregator.Aggregate(diffs, maxSamples: 1);
        var breakingApiCount = apiGroups.Count(group =>
            group.Patterns.Any(pattern => pattern.ChangeType is FieldRemoved or TypeChanged)
        );

        var total = compare.TotalRequest ?? 0;
        var unchanged = compare.UnchangedCount ?? 0;

        var report = new DomainEntities.Report
        {
            ProjectId = projectId.Value,
            CompareJobId = compare.Id,
            CompareResultId = resultId,
            Name = $"Regression Report — {compare.RunATitle} vs {compare.RunBTitle}",
            Status = ReportStatuses.Completed,
            RegressionScore = ComputeScore(unchanged, total),
            RiskLevel = ComputeRiskLevel(breakingApiCount),
            TotalRequest = total,
            UnchangedCount = unchanged,
            ChangedCount = compare.ChangedCount ?? 0,
            AddedCount = compare.AddedCount ?? 0,
            RemovedCount = compare.RemovedCount ?? 0,
            FailedCount = compare.FailedCount ?? 0,
            TotalApis = apiStats.Count,
            BreakingApiCount = breakingApiCount,
        };

        var id = await reportRepository.UpsertAsync(report, cancellationToken);

        var saved = await reportRepository.GetByIdAsync(id, cancellationToken);
        if (saved is null)
        {
            return Result<ReportListItem>.Failure(new Error("Report.GenerateFailed", "Failed to generate the report."));
        }

        var listItem = new ReportListItem(
            saved.Id,
            saved.CompareJobId,
            saved.RunAId,
            saved.RunBId,
            saved.RunATitle,
            saved.RunBTitle,
            saved.Name,
            saved.Status,
            saved.RegressionScore,
            saved.RiskLevel,
            saved.TotalRequest,
            saved.UnchangedCount,
            saved.ChangedCount,
            saved.AddedCount,
            saved.RemovedCount,
            saved.FailedCount,
            saved.TotalApis,
            saved.BreakingApiCount,
            saved.GeneratedDate,
            saved.CreatedDate
        );
        return Result<ReportListItem>.Success(listItem);
    }

    /// <summary>Unchanged share of the compare, 0–100 (100 when there was nothing to compare).</summary>
    private static decimal ComputeScore(int unchanged, int total) =>
        total <= 0 ? 100m : Math.Round((decimal)unchanged / total * 100m, 1);

    /// <summary>Risk from the number of APIs with backward-incompatible changes (tunable heuristic).</summary>
    private static string ComputeRiskLevel(int breakingApiCount) =>
        breakingApiCount switch
        {
            0 => ReportRiskLevels.Low,
            <= 2 => ReportRiskLevels.Medium,
            <= 5 => ReportRiskLevels.High,
            _ => ReportRiskLevels.Critical,
        };
}
