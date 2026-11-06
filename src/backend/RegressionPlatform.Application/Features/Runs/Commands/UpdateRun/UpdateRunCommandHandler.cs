using MediatR;
using RegressionPlatform.Application.Features.Datasets;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.Commands.UpdateRun;

public sealed class UpdateRunCommandHandler(
    IRunRepository runRepository,
    IProjectRepository projectRepository,
    IDatasetRepository datasetRepository
) : IRequestHandler<UpdateRunCommand, Result<RunResponse>>
{
    public async Task<Result<RunResponse>> Handle(UpdateRunCommand request, CancellationToken cancellationToken)
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<RunResponse>.NotFound(new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist."));
        }

        var run = await runRepository.GetEntityAsync(request.RunId, cancellationToken);
        if (run is null || run.ProjectId != projectId.Value)
        {
            return Result<RunResponse>.NotFound(new Error("Run.NotFound", $"Run {request.RunId} does not exist in the project."));
        }

        // Only a run that has never executed can be edited.
        if (!string.Equals(run.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            return Result<RunResponse>.Conflict(new Error("Run.NotPending", "Only a pending run (not yet started) can be edited."));
        }

        // The target dataset must belong to this project and be active (same rule as create).
        var dataset = await datasetRepository.GetEntityAsync(request.DatasetId, cancellationToken);
        if (dataset is null || dataset.ProjectId != projectId.Value)
        {
            return Result<RunResponse>.NotFound(new Error("Dataset.NotFound", $"Dataset {request.DatasetId} was not found in this project."));
        }
        if (!dataset.IsActive)
        {
            return Result<RunResponse>.Conflict(new Error("Dataset.Inactive", "This dataset is inactive and cannot be run."));
        }

        run.DatasetId = request.DatasetId;
        run.Title = request.Title;
        run.Description = request.Description;
        run.Environment = request.Environment;
        run.RunType = request.RunType;
        run.RequestLimitPerApi = request.RequestLimitPerApi;
        run.MaxParallelApis = request.MaxParallelApis;
        run.MaxParallelRequestsPerApi = request.MaxParallelRequestsPerApi;

        var updated = await runRepository.UpdateAsync(run, request.ApiIds, cancellationToken);
        if (!updated)
        {
            return Result<RunResponse>.Conflict(new Error("Run.NotPending", "Only a pending run (not yet started) can be edited."));
        }

        var response = await runRepository.GetByIdAsync(request.RunId, cancellationToken);
        return response is null
            ? Result<RunResponse>.Failure(new Error("Run.UpdateFailed", "Failed to update the run."))
            : Result<RunResponse>.Success(response);
    }
}
