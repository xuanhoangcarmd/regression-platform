using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.ExpectedResponses.RestoreExpectedResponses;

public sealed class RestoreExpectedResponsesCommandHandler(
    IExpectedResponseRepository repository,
    IRunRepository runRepository,
    IProjectRepository projectRepository
) : IRequestHandler<RestoreExpectedResponsesCommand, Result<int>>
{
    public async Task<Result<int>> Handle(RestoreExpectedResponsesCommand request, CancellationToken cancellationToken)
    {
        var (run, error) = await ExpectedResponseRunGuard.FindAsync(
            projectRepository, runRepository, request.ProjectCode, request.RunId, cancellationToken);
        if (error is not null)
        {
            return Result<int>.NotFound(error);
        }

        if (ExpectedResponseRunGuard.EnsureEditable(run!) is { } notFinished)
        {
            return Result<int>.Conflict(notFinished);
        }

        var removed = await repository.DeleteAsync(request.RunId, request.RequestIds.Distinct().ToList(), cancellationToken);
        return Result<int>.Success(removed);
    }
}
