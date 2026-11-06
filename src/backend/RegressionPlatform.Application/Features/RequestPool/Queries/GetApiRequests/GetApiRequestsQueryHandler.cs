using MediatR;
using RegressionPlatform.Application.Features.Datasets;
using RegressionPlatform.Application.Features.RequestPool.Contracts;
using RegressionPlatform.Shared.Pagination;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.RequestPool.Queries.GetApiRequests;

public sealed class GetApiRequestsQueryHandler(IApiRequestRepository repository, IDatasetRepository datasetRepository)
    : IRequestHandler<GetApiRequestsQuery, Result<PagedResult<ApiRequestListItemResponse>>>
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<Result<PagedResult<ApiRequestListItemResponse>>> Handle(
        GetApiRequestsQuery request,
        CancellationToken cancellationToken
    )
    {
        var dataset = await datasetRepository.GetEntityAsync(request.DatasetId, cancellationToken);
        if (dataset is null)
        {
            return Result<PagedResult<ApiRequestListItemResponse>>.NotFound(
                new Error("Dataset.NotFound", $"Dataset {request.DatasetId} was not found.")
            );
        }

        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);

        var parameters = new ApiRequestListParameters(
            request.DatasetId,
            request.ApiId,
            request.Search,
            request.IsActive,
            request.SortBy,
            request.SortDirection,
            pageNumber,
            pageSize
        );

        var page = await repository.GetPagedAsync(parameters, cancellationToken);
        return Result<PagedResult<ApiRequestListItemResponse>>.Success(page);
    }
}
