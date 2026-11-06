using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Datasets.Commands.DeleteDataset;

public sealed class DeleteDatasetCommandHandler(IDatasetRepository repository)
    : IRequestHandler<DeleteDatasetCommand, Result>
{
    public async Task<Result> Handle(DeleteDatasetCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.GetEntityAsync(request.Id, cancellationToken);
        if (existing is null)
        {
            return Result.NotFound(new Error("Dataset.NotFound", $"Dataset {request.Id} was not found."));
        }

        if (await repository.HasRunsAsync(request.Id, cancellationToken))
        {
            return Result.Conflict(
                new Error(
                    "Dataset.InUse",
                    "This dataset is used by one or more runs. Delete those runs first, or set the dataset to Inactive instead of deleting it."
                )
            );
        }

        // No runs reference it → safe to delete the dataset together with its requests.
        await repository.DeleteAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
