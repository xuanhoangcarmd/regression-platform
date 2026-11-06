using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.Commands.RerunRequest;

public sealed class RerunRequestCommandHandler(
    IRunRepository runRepository,
    IProjectRepository projectRepository,
    IRunRequestExecutor requestExecutor
) : IRequestHandler<RerunRequestCommand, Result>
{
    public async Task<Result> Handle(RerunRequestCommand request, CancellationToken cancellationToken)
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

        // Can't re-run a single request while the whole run is still executing.
        if (string.Equals(run.Status, "Running", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Conflict(new Error("Run.Running", "The run is still executing; wait for it to finish before re-running a request."));
        }

        var ok = await requestExecutor.RerunRequestAsync(request.RunId, request.RequestId, cancellationToken);
        return ok
            ? Result.Success()
            : Result.Conflict(new Error(
                "Run.RerunFailed",
                "Could not re-run the request — it may no longer exist or the environment has no Base URL configured."
            ));
    }
}
