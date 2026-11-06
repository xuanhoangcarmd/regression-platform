using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.Commands.StopRun;

public sealed class StopRunCommandHandler(
    IRunRepository runRepository,
    IProjectRepository projectRepository,
    IRunCancellationRegistry cancellationRegistry
) : IRequestHandler<StopRunCommand, Result>
{
    public async Task<Result> Handle(StopRunCommand request, CancellationToken cancellationToken)
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

        // Mark Cancelled first (guarded Queued/Running → Cancelled), then signal the worker to abort.
        // Doing the DB update before cancelling means the executor sees the final state when its token
        // trips. A cancelled Queued run is skipped when the worker later dequeues it.
        var cancelled = await runRepository.MarkCancelledAsync(request.RunId, DateTime.UtcNow, cancellationToken);
        if (!cancelled)
        {
            return Result.Conflict(new Error("Run.NotRunning", "Run is not queued or running and cannot be stopped."));
        }

        cancellationRegistry.Cancel(request.RunId);
        return Result.Success();
    }
}
