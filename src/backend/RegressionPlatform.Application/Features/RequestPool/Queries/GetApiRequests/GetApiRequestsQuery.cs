using MediatR;
using RegressionPlatform.Application.Features.RequestPool.Contracts;
using RegressionPlatform.Shared.Pagination;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.RequestPool.Queries.GetApiRequests;

/// <summary>Lists a dataset's requests with API filter, title search, status filter, sorting, paging.</summary>
public sealed record GetApiRequestsQuery(
    int DatasetId,
    int? ApiId = null,
    string? Search = null,
    bool? IsActive = null,
    string? SortBy = null,
    string? SortDirection = null,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<Result<PagedResult<ApiRequestListItemResponse>>>;
