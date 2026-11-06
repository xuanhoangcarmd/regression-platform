# Purpose

Standardize data tables and their sorting so lists are consistent, performant, type-safe,
and aligned with the backend pagination/sorting contract.

# Rules

- Use Angular Material `mat-table` (`MatTableModule`) for tabular data; import it standalone in the component.
- Strongly type the table data and column definitions; never use `any` for rows.
- Define columns declaratively and drive `displayedColumns` from a typed config; avoid hardcoding column markup ad hoc.
- Use the `async` pipe or signal-bound data sources; do not manually push into `MatTableDataSource` from a `.subscribe()`.
- Use `@for` with `track` for any custom (non-Material) row rendering.

## Sorting

- Use `matSort` / `MatSortModule` with `mat-sort-header` on sortable columns.
- **Server-side sort for paginated data**: listen to `(matSortChange)`, send `sortBy` and `sortDirection` to the API (matching the backend contract), and reset to page 1 on sort change. Do not sort the full set client-side when data is paginated.
- Client-side sort only for small, fully-loaded datasets.
- Keep sort state (`sortBy`, `sortDirection`) in the feature service alongside paging/filter state.

## Pagination

- Use `mat-paginator` and wire it to the API pagination params (`pageNumber`, `pageSize`, `totalCount`) per the pagination rules.
- Keep paging, sorting, and filtering in one place in the service so they compose into a single request.

## Presentation

- Style layout/spacing with Tailwind utilities; use Material theming for table chrome.
- Show loading feedback (skeleton rows or overlay spinner) per the loading rules, and render an explicit empty state when there are no rows.

# AI Generation Requirements

When generating tables, the AI **must**:

- Use `mat-table` with typed rows/columns and a typed `displayedColumns` config.
- Implement server-side sorting/pagination for paginated data using `matSort`/`mat-paginator`, sending the API's `sortBy`/`sortDirection`/`pageNumber`/`pageSize` and resetting to page 1 on sort change.
- Keep sort/page/filter state in the service and show loading + empty states.

The AI **must never**:

- Use `any` for rows or hardcode column markup without a typed config.
- Sort/paginate a paginated dataset entirely on the client.
- Manually `.subscribe()` to push rows into the data source.
