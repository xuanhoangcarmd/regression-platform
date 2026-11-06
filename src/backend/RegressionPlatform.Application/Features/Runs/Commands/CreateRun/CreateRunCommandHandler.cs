using MediatR;
using RegressionPlatform.Application.Features.Datasets;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Shared.Results;
using DomainEntities = RegressionPlatform.Domain.Entities;

namespace RegressionPlatform.Application.Features.Runs.Commands.CreateRun;

public sealed class CreateRunCommandHandler(
    IRunRepository repository,
    IProjectRepository projectRepository,
    IDatasetRepository datasetRepository
) : IRequestHandler<CreateRunCommand, Result<RunResponse>>
{
    private const string PendingStatus = "Pending";

    public async Task<Result<RunResponse>> Handle(CreateRunCommand request, CancellationToken cancellationToken)
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<RunResponse>.NotFound(new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist."));
        }

        // The run must execute a dataset that belongs to this project and is active.
        var dataset = await datasetRepository.GetEntityAsync(request.DatasetId, cancellationToken);
        if (dataset is null || dataset.ProjectId != projectId.Value)
        {
            return Result<RunResponse>.NotFound(new Error("Dataset.NotFound", $"Dataset {request.DatasetId} was not found in this project."));
        }

        if (!dataset.IsActive)
        {
            return Result<RunResponse>.Conflict(new Error("Dataset.Inactive", "This dataset is inactive and cannot be run."));
        }

        var run = new DomainEntities.Run
        {
            ProjectId = projectId.Value,
            DatasetId = request.DatasetId,
            Title = request.Title,
            Description = request.Description,
            Environment = request.Environment,
            RunType = request.RunType,
            RequestLimitPerApi = request.RequestLimitPerApi,
            MaxParallelApis = request.MaxParallelApis,
            MaxParallelRequestsPerApi = request.MaxParallelRequestsPerApi,
            Status = PendingStatus,
        };

        var id = await repository.CreateAsync(run, request.ApiIds, cancellationToken);

        // Re-read so the response reflects DB-applied defaults (CreatedDate).
        var created = await repository.GetByIdAsync(id, cancellationToken);
        return created is null
            ? Result<RunResponse>.Failure(new Error("Run.CreateFailed", "Failed to create the run."))
            : Result<RunResponse>.Success(created);
    }
}
