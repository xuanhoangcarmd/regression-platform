using MediatR;
using RegressionPlatform.Application.Features.Compare.Contracts;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Compare.Queries.GetCompareApiStatistics;

public sealed class GetCompareApiStatisticsQueryHandler(
    ICompareRepository repository,
    IProjectRepository projectRepository
) : IRequestHandler<GetCompareApiStatisticsQuery, Result<IReadOnlyList<CompareApiStatistic>>>
{
    public async Task<Result<IReadOnlyList<CompareApiStatistic>>> Handle(
        GetCompareApiStatisticsQuery request,
        CancellationToken cancellationToken
    )
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<IReadOnlyList<CompareApiStatistic>>.NotFound(
                new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist.")
            );
        }

        var job = await repository.GetJobAsync(request.CompareId, cancellationToken);
        if (job is null || job.ProjectId != projectId.Value)
        {
            return Result<IReadOnlyList<CompareApiStatistic>>.NotFound(
                new Error("Compare.NotFound", $"Compare {request.CompareId} does not exist in the project.")
            );
        }

        if (job.CompareResultId is not long resultId)
        {
            // Not finished yet — no breakdown to show.
            return Result<IReadOnlyList<CompareApiStatistic>>.Success([]);
        }

        var statistics = await repository.GetApiStatisticsAsync(resultId, cancellationToken);
        return Result<IReadOnlyList<CompareApiStatistic>>.Success(statistics);
    }
}
