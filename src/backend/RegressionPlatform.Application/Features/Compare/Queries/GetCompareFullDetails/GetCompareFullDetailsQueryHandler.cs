using MediatR;
using RegressionPlatform.Application.Features.Compare.Contracts;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Compare.Queries.GetCompareFullDetails;

public sealed class GetCompareFullDetailsQueryHandler(ICompareRepository repository, IProjectRepository projectRepository)
    : IRequestHandler<GetCompareFullDetailsQuery, Result<IReadOnlyList<CompareFullDetailItem>>>
{
    public async Task<Result<IReadOnlyList<CompareFullDetailItem>>> Handle(
        GetCompareFullDetailsQuery request,
        CancellationToken cancellationToken
    )
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<IReadOnlyList<CompareFullDetailItem>>.NotFound(
                new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist.")
            );
        }

        var job = await repository.GetJobAsync(request.CompareId, cancellationToken);
        if (job is null || job.ProjectId != projectId.Value)
        {
            return Result<IReadOnlyList<CompareFullDetailItem>>.NotFound(
                new Error("Compare.NotFound", $"Compare {request.CompareId} does not exist in the project.")
            );
        }

        if (job.CompareResultId is not long resultId)
        {
            return Result<IReadOnlyList<CompareFullDetailItem>>.Success([]); // not finished yet
        }

        var details = await repository.GetFullDetailsAsync(resultId, cancellationToken);
        return Result<IReadOnlyList<CompareFullDetailItem>>.Success(details);
    }
}
