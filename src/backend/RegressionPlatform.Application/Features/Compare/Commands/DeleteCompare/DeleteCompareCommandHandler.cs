using MediatR;
using RegressionPlatform.Application.Features.Compare.Contracts;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Compare.Commands.DeleteCompare;

public sealed class DeleteCompareCommandHandler(ICompareRepository repository, IProjectRepository projectRepository)
    : IRequestHandler<DeleteCompareCommand, Result>
{
    public async Task<Result> Handle(DeleteCompareCommand request, CancellationToken cancellationToken)
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

        // A running compare is still writing rows — don't delete it mid-execution (no cancel channel).
        if (job.Status == CompareStatuses.Running)
        {
            return Result.Conflict(new Error(
                "Compare.Running",
                "This compare is still running. Wait for it to finish before deleting it."
            ));
        }

        // A compare snapshotted into a report can't be deleted (it would break that report's history).
        if (await repository.HasReportAsync(request.CompareId, cancellationToken))
        {
            return Result.Conflict(new Error(
                "Compare.HasReport",
                "This compare is used by a report and cannot be deleted. Delete the report first."
            ));
        }

        await repository.DeleteAsync(request.CompareId, cancellationToken);
        return Result.Success();
    }
}
