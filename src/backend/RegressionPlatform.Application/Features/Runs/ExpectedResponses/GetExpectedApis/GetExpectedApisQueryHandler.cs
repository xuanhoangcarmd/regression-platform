using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.ExpectedResponses.GetExpectedApis;

public sealed class GetExpectedApisQueryHandler(
    IExpectedResponseRepository repository,
    IRunRepository runRepository,
    IProjectRepository projectRepository
) : IRequestHandler<GetExpectedApisQuery, Result<IReadOnlyList<ExpectedApiItem>>>
{
    public async Task<Result<IReadOnlyList<ExpectedApiItem>>> Handle(GetExpectedApisQuery request, CancellationToken cancellationToken)
    {
        var (_, error) = await ExpectedResponseRunGuard.FindAsync(
            projectRepository, runRepository, request.ProjectCode, request.RunId, cancellationToken);
        if (error is not null)
        {
            return Result<IReadOnlyList<ExpectedApiItem>>.NotFound(error);
        }

        return Result<IReadOnlyList<ExpectedApiItem>>.Success(await repository.GetApisAsync(request.RunId, cancellationToken));
    }
}
