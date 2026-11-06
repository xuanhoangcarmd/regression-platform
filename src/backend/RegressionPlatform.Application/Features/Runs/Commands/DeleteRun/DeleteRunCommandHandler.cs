using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.Commands.DeleteRun;

public sealed class DeleteRunCommandHandler(
    IRunRepository runRepository,
    IProjectRepository projectRepository,
    IRunCancellationRegistry cancellationRegistry
) : IRequestHandler<DeleteRunCommand, Result>
{
    public async Task<Result> Handle(DeleteRunCommand request, CancellationToken cancellationToken)
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result.NotFound(new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist."));
        }

        var run = await runRepository.GetEntityAsync(request.RunId, cancellationToken);
        if (run is null || run.ProjectId != projectId.Value)
        {
            return Result.NotFound(new Error("Run.NotFound", $"Run {request.RunId} does not exist in the project."));
        }

        // A run used in a comparison cannot be deleted (deleting it would break that compare's history).
        if (await runRepository.HasCompareAsync(request.RunId, cancellationToken))
        {
            return Result.Conflict(new Error(
                "Run.HasCompare",
                "This run is used in one or more comparisons and cannot be deleted. Delete those comparisons first."
            ));
        }

        // If it's currently executing, signal the worker to stop first so it isn't writing rows into a
        // run we're about to delete, then remove the run and its responses.
        cancellationRegistry.Cancel(request.RunId);
        await runRepository.DeleteAsync(request.RunId, cancellationToken);
        return Result.Success();
    }
}
