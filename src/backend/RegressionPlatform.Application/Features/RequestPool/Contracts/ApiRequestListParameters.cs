namespace RegressionPlatform.Application.Features.RequestPool.Contracts;

/// <summary>Normalized filter/sort/paging inputs for the Request Pool list.</summary>
public sealed record ApiRequestListParameters(
    int DatasetId,
    int? ApiId,
    string? Search,
    bool? IsActive,
    string? SortBy,
    string? SortDirection,
    int PageNumber,
    int PageSize
);
