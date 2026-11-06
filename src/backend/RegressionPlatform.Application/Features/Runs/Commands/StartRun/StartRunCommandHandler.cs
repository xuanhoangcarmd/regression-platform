using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.Commands.StartRun;

public sealed class StartRunCommandHandler(
    IRunRepository runRepository,
    IProjectRepository projectRepository,
    IRunExecutionQueue executionQueue
) : IRequestHandler<StartRunCommand, Result>
{
    public async Task<Result> Handle(StartRunCommand request, CancellationToken cancellationToken)
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

        // Atomically Pending → Queued (guards double-start under concurrency). The background worker
        // marks it Running when execution actually begins — runs waiting behind another run show as
        // Queued instead of a misleading Running.
        var started = await runRepository.MarkQueuedAsync(request.RunId, cancellationToken);
        if (!started)
        {
            return Result.Conflict(new Error("Run.NotPending", "Run is not in Pending status and cannot be started."));
        }

        await executionQueue.EnqueueAsync(request.RunId, cancellationToken);
        return Result.Success();
    }
}
