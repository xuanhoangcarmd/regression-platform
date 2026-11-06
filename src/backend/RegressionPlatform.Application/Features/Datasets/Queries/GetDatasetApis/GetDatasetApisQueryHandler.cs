using MediatR;
using RegressionPlatform.Application.Features.Datasets.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Datasets.Queries.GetDatasetApis;

public sealed class GetDatasetApisQueryHandler(IDatasetRepository repository)
    : IRequestHandler<GetDatasetApisQuery, Result<IReadOnlyList<DatasetApiResponse>>>
{
    public async Task<Result<IReadOnlyList<DatasetApiResponse>>> Handle(
        GetDatasetApisQuery request,
        CancellationToken cancellationToken
    )
    {
        var dataset = await repository.GetEntityAsync(request.DatasetId, cancellationToken);
        if (dataset is null)
        {
            return Result<IReadOnlyList<DatasetApiResponse>>.NotFound(
                new Error("Dataset.NotFound", $"Dataset {request.DatasetId} was not found.")
            );
        }

        var apis = await repository.GetApisAsync(request.DatasetId, cancellationToken);
        return Result<IReadOnlyList<DatasetApiResponse>>.Success(apis);
    }
}
