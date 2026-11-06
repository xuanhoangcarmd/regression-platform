namespace RegressionPlatform.Shared.Pagination;

/// <summary>
/// Standard paged response (matches the API/frontend contract — see pagination.md):
/// items + pageNumber + pageSize + totalCount, with totalPages derived.
/// </summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
