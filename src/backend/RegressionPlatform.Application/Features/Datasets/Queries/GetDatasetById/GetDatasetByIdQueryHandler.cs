using MediatR;
using RegressionPlatform.Application.Features.Datasets.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Datasets.Queries.GetDatasetById;

public sealed class GetDatasetByIdQueryHandler(IDatasetRepository repository)
    : IRequestHandler<GetDatasetByIdQuery, Result<DatasetResponse>>
{
    public async Task<Result<DatasetResponse>> Handle(GetDatasetByIdQuery request, CancellationToken cancellationToken)
    {
        var dataset = await repository.GetByIdAsync(request.Id, cancellationToken);
        return dataset is null
            ? Result<DatasetResponse>.NotFound(new Error("Dataset.NotFound", $"Dataset {request.Id} was not found."))
            : Result<DatasetResponse>.Success(dataset);
    }
}
