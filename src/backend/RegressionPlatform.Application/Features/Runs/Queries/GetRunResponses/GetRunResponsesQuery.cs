using MediatR;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Shared.Pagination;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.Queries.GetRunResponses;

/// <summary>Lists a run's executed requests (Requests tab) with search + status filter and paging.</summary>
public sealed record GetRunResponsesQuery(
    string ProjectCode,
    long RunId,
    string? Search = null,
    string? Status = null,
    int PageNumber = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDirection = null,
    int? ApiId = null
) : IRequest<Result<PagedResult<RunResponseListItem>>>;
