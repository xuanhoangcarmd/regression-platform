using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.Commands.RerunRun;

public sealed class RerunRunCommandHandler(
    IRunRepository runRepository,
    IProjectRepository projectRepository,
    IRunExecutionQueue executionQueue
) : IRequestHandler<RerunRunCommand, Result>
{
    public async Task<Result> Handle(RerunRunCommand request, CancellationToken cancellationToken)
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

        // A compare reads this run's stored responses; replacing them would silently change its history.
        if (await runRepository.HasCompareAsync(request.RunId, cancellationToken))
        {
            return Result.Conflict(new Error(
                "Run.HasCompare",
                "This run is used in one or more comparisons and cannot be re-run. Delete those comparisons first, or create a new run."
            ));
        }

        // Atomically Completed/Failed/Cancelled → Queued (+ old responses removed). False = it is
        // still Pending/Queued/Running, or another re-run already claimed it.
        var reset = await runRepository.ResetForRerunAsync(request.RunId, cancellationToken);
        if (!reset)
        {
            return Result.Conflict(new Error(
                "Run.NotFinished",
                "Only a Completed, Failed or Cancelled run can be re-run."
            ));
        }

        await executionQueue.EnqueueAsync(request.RunId, cancellationToken);
        return Result.Success();
    }
}
