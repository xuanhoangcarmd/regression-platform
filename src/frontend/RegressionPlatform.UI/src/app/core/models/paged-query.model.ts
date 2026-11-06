export type SortDirection = 'asc' | 'desc';

/**
 * Common query params for list endpoints: pagination, sorting, filtering.
 * Mirrors the backend contract — see docs/rules/dotnet/api-design.md.
 */
export interface IPagedQuery {
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: SortDirection;
}

/** Pagination defaults/limits (must match the backend, pagination.md). */
export const PaginationDefaults = {
  pageNumber: 1,
  pageSize: 20,
  maxPageSize: 100,
} as const;
