using MediatR;
using RegressionPlatform.Application.Features.Projects.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Projects.Queries.GetProjects;

public sealed class GetProjectsQueryHandler(IProjectRepository repository)
    : IRequestHandler<GetProjectsQuery, Result<IReadOnlyList<ProjectResponse>>>
{
    public async Task<Result<IReadOnlyList<ProjectResponse>>> Handle(
        GetProjectsQuery request,
        CancellationToken cancellationToken
    )
    {
        var projects = await repository.GetAllAsync(cancellationToken);
        return Result<IReadOnlyList<ProjectResponse>>.Success(projects);
    }
}
