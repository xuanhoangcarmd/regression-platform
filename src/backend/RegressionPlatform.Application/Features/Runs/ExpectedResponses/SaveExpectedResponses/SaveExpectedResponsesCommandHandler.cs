using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.ExpectedResponses.SaveExpectedResponses;

public sealed class SaveExpectedResponsesCommandHandler(
    IExpectedResponseRepository repository,
    IRunRepository runRepository,
    IProjectRepository projectRepository
) : IRequestHandler<SaveExpectedResponsesCommand, Result<int>>
{
    public async Task<Result<int>> Handle(SaveExpectedResponsesCommand request, CancellationToken cancellationToken)
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

        // Only requests executed in this run can get an expected version.
        var ids = request.Items.Select(item => item.RequestId).ToList();
        var existing = await repository.CountResponsesAsync(request.RunId, ids, cancellationToken);
        if (existing != ids.Count)
        {
            return Result<int>.NotFound(new Error(
                "RunResponse.NotFound",
                $"{ids.Count - existing} of the requests have no response in run {request.RunId}."
            ));
        }

        // No authentication yet → the editor is not recorded (CreatedBy/UpdatedBy stay null).
        var saved = await repository.SaveAsync(request.RunId, request.Items, userName: null, cancellationToken);
        return Result<int>.Success(saved);
    }
}
