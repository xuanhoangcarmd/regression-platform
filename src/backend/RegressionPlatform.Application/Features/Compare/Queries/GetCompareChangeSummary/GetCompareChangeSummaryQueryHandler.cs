using MediatR;
using RegressionPlatform.Application.Features.Compare.Contracts;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Compare.Queries.GetCompareChangeSummary;

public sealed class GetCompareChangeSummaryQueryHandler(ICompareRepository repository, IProjectRepository projectRepository)
    : IRequestHandler<GetCompareChangeSummaryQuery, Result<CompareChangeSummaryResponse>>
{
    /// <summary>Representative old→new samples kept per pattern.</summary>
    private const int MaxSamplesPerPattern = 3;

    public async Task<Result<CompareChangeSummaryResponse>> Handle(
        GetCompareChangeSummaryQuery request,
        CancellationToken cancellationToken
    )
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<CompareChangeSummaryResponse>.NotFound(
                new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist.")
            );
        }

        var summary = await repository.GetSummaryAsync(request.Id, cancellationToken);
        if (summary is null || summary.ProjectId != projectId.Value)
        {
            return Result<CompareChangeSummaryResponse>.NotFound(
                new Error("Compare.NotFound", $"Compare {request.Id} does not exist in the project.")
            );
        }

        var totals = new CompareChangeTotals(
            summary.TotalRequest ?? 0,
            summary.UnchangedCount ?? 0,
            summary.ChangedCount ?? 0,
            summary.AddedCount ?? 0,
            summary.RemovedCount ?? 0,
            summary.FailedCount ?? 0
        );

        // Patterns only exist once the compare has finished (has a result); grouped per API.
        var apis = summary.CompareResultId is long resultId
            ? ChangeSummaryAggregator.Aggregate(
                await repository.GetDiffsAsync(resultId, cancellationToken),
                MaxSamplesPerPattern
            )
            : [];

        var response = new CompareChangeSummaryResponse(
            summary.Id,
            summary.RunATitle,
            summary.RunBTitle,
            totals,
            apis
        );
        return Result<CompareChangeSummaryResponse>.Success(response);
    }
}
