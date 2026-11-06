using MediatR;
using RegressionPlatform.Application.Features.ApiDefinition.Contracts;
using RegressionPlatform.Shared.Pagination;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.ApiDefinition.Queries.GetApis;

/// <summary>Lists a project's API definitions with search, status filter, sorting, and pagination.</summary>
public sealed record GetApisQuery(
    string ProjectCode,
    string? Search = null,
    bool? IsActive = null,
    string? SortBy = null,
    string? SortDirection = null,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<Result<PagedResult<ApiResponse>>>;
