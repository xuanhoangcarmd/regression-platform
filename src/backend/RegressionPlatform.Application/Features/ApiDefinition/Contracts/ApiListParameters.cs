namespace RegressionPlatform.Application.Features.ApiDefinition.Contracts;

/// <summary>Normalized filter/sort/paging inputs passed from the query handler to the repository.</summary>
public sealed record ApiListParameters(
    int ProjectId,
    string? Search,
    bool? IsActive,
    string? SortBy,
    string? SortDirection,
    int PageNumber,
    int PageSize
);
