using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Compare.Commands.StopCompare;

public sealed class StopCompareCommandHandler(
    ICompareRepository repository,
    IProjectRepository projectRepository,
    ICompareCancellationRegistry cancellationRegistry
) : IRequestHandler<StopCompareCommand, Result>
{
    public async Task<Result> Handle(StopCompareCommand request, CancellationToken cancellationToken)
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result.NotFound(new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist."));
        }

        var job = await repository.GetJobAsync(request.CompareId, cancellationToken);
        if (job is null || job.ProjectId != projectId.Value)
        {
            return Result.NotFound(
                new Error("Compare.NotFound", $"Compare {request.CompareId} does not exist in the project.")
            );
        }

        // Mark Cancelled first (guarded Running → Cancelled), then signal the worker to abort. Doing the
        // DB update before cancelling means the executor sees the final state when its token trips, and
        // a still-queued job is skipped when the worker later dequeues it (guarded MarkRunning).
        var cancelled = await repository.MarkCancelledAsync(request.CompareId, DateTime.UtcNow, cancellationToken);
        if (!cancelled)
        {
            return Result.Conflict(new Error("Compare.NotRunning", "Compare is not running and cannot be stopped."));
        }

        cancellationRegistry.Cancel(request.CompareId);
        return Result.Success();
    }
}
