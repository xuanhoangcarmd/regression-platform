using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Datasets.Commands.UpdateDataset;

public sealed class UpdateDatasetCommandHandler(IDatasetRepository repository)
    : IRequestHandler<UpdateDatasetCommand, Result>
{
    public async Task<Result> Handle(UpdateDatasetCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.GetEntityAsync(request.Id, cancellationToken);
        if (existing is null)
        {
            return Result.NotFound(new Error("Dataset.NotFound", $"Dataset {request.Id} was not found."));
        }

        var name = request.Name.Trim();
        if (await repository.ExistsByNameAsync(existing.ProjectId, name, request.Id, cancellationToken))
        {
            return Result.Conflict(new Error("Dataset.DuplicateName", $"A dataset named '{name}' already exists in this project."));
        }

        existing.Name = name;
        existing.Description = request.Description;
        existing.IsActive = request.IsActive;

        await repository.UpdateAsync(existing, cancellationToken);
        return Result.Success();
    }
}
