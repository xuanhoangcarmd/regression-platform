using MediatR;
using RegressionPlatform.Application.Features.Compare.Contracts;
using RegressionPlatform.Shared.Pagination;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Compare.Queries.GetCompareDetails;

/// <summary>Paged per-request detail list for a compare, with search + result filter and sorting.</summary>
public sealed record GetCompareDetailsQuery(
    string ProjectCode,
    long CompareId,
    string? Search = null,
    string? Result = null,
    int PageNumber = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDirection = null,
    int? ApiId = null,
    // Filter to requests whose field-diff contains this change pattern (from the AI Insights list).
    // ChangePath is the index-normalized path (e.g. $.data.Monitors[*].status); both are optional.
    string? ChangeType = null,
    string? ChangePath = null
) : IRequest<Result<PagedResult<CompareDetailListItem>>>;
