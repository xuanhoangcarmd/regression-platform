/**
 * Standard paginated response shape returned by all list endpoints.
 * Mirrors the backend contract — see docs/rules/dotnet/pagination.md.
 */
export interface IPagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
