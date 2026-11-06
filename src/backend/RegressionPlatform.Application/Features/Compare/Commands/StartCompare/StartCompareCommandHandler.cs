using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Compare.Commands.StartCompare;

public sealed class StartCompareCommandHandler(
    ICompareRepository repository,
    IProjectRepository projectRepository,
    ICompareExecutionQueue executionQueue
) : IRequestHandler<StartCompareCommand, Result>
{
    public async Task<Result> Handle(StartCompareCommand request, CancellationToken cancellationToken)
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result.NotFound(new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist."));
        }

        var job = await repository.GetJobAsync(request.Id, cancellationToken);
        if (job is null || job.ProjectId != projectId.Value)
        {
            return Result.NotFound(new Error("Compare.NotFound", $"Compare {request.Id} does not exist in the project."));
        }

        // Atomically Pending → Running (guards double-start under concurrency).
        var started = await repository.TryStartAsync(request.Id, DateTime.UtcNow, cancellationToken);
        if (!started)
        {
            return Result.Conflict(new Error("Compare.NotPending", "Compare is not in Pending status and cannot be started."));
        }

        await executionQueue.EnqueueAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
