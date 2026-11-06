using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.ExpectedResponses.GetExpectedRequests;

public sealed class GetExpectedRequestsQueryHandler(
    IExpectedResponseRepository repository,
    IRunRepository runRepository,
    IProjectRepository projectRepository
) : IRequestHandler<GetExpectedRequestsQuery, Result<IReadOnlyList<ExpectedRequestItem>>>
{
    public async Task<Result<IReadOnlyList<ExpectedRequestItem>>> Handle(
        GetExpectedRequestsQuery request,
        CancellationToken cancellationToken
    )
    {
        var (_, error) = await ExpectedResponseRunGuard.FindAsync(
            projectRepository, runRepository, request.ProjectCode, request.RunId, cancellationToken);
        if (error is not null)
        {
            return Result<IReadOnlyList<ExpectedRequestItem>>.NotFound(error);
        }

        return Result<IReadOnlyList<ExpectedRequestItem>>.Success(
            await repository.GetRequestsAsync(request.RunId, request.ApiId, cancellationToken));
    }
}
