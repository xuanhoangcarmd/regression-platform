using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Shared.Results;
using DomainEntities = RegressionPlatform.Domain.Entities;

namespace RegressionPlatform.Application.Features.Datasets.Commands.CreateDataset;

public sealed class CreateDatasetCommandHandler(IDatasetRepository repository, IProjectRepository projectRepository)
    : IRequestHandler<CreateDatasetCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CreateDatasetCommand request, CancellationToken cancellationToken)
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<int>.NotFound(new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist."));
        }

        var name = request.Name.Trim();
        if (await repository.ExistsByNameAsync(projectId.Value, name, null, cancellationToken))
        {
            return Result<int>.Conflict(new Error("Dataset.DuplicateName", $"A dataset named '{name}' already exists in this project."));
        }

        var dataset = new DomainEntities.Dataset
        {
            ProjectId = projectId.Value,
            Name = name,
            Description = request.Description,
            IsActive = true,
        };

        var id = await repository.CreateAsync(dataset, cancellationToken);
        return Result<int>.Success(id);
    }
}
