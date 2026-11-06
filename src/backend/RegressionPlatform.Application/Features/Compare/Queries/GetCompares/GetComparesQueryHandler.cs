using MediatR;
using RegressionPlatform.Application.Features.Compare.Contracts;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Compare.Queries.GetCompares;

public sealed class GetComparesQueryHandler(ICompareRepository repository, IProjectRepository projectRepository)
    : IRequestHandler<GetComparesQuery, Result<IReadOnlyList<CompareJobListItem>>>
{
    public async Task<Result<IReadOnlyList<CompareJobListItem>>> Handle(
        GetComparesQuery request,
        CancellationToken cancellationToken
    )
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<IReadOnlyList<CompareJobListItem>>.NotFound(
                new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist.")
            );
        }

        var jobs = await repository.GetJobsByProjectAsync(projectId.Value, cancellationToken);
        return Result<IReadOnlyList<CompareJobListItem>>.Success(jobs);
    }
}
