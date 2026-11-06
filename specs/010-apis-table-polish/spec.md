# Feature Specification: API Table Visual Polish

**Feature Directory**: `specs/010-apis-table-polish`
**Created**: 2026-06-21
**Status**: Draft

> Focus on **WHAT** and **WHY**, not HOW. Written for stakeholders, not implementers.
> Remove any section that does not apply (do not leave "N/A").

---

## Summary

Refine the data table on the **API management** screen to match the approved sample: a clean,
airy table with a light header, blue linked IDs, plain-text method, monospace endpoint,
right-aligned request counts, a small green status pill, borderless gray row actions, subtle row
dividers with hover, and a footer showing a result summary plus a boxed numbered pager. This is a
visual-polish pass only — the table's data and behaviour (search, filter, paging, edit/delete) are
unchanged.

---

## User Scenarios & Testing *(mandatory)*

### Primary User Flow

1. A user opens the API management screen and sees the table styled like the sample.
2. The header row is lightly shaded with muted column labels: **API ID, Tên API, Method, Endpoint, Requests, Trạng thái, Actions**.
3. Each row shows: a **blue linked API ID**, the API name, the **method as plain text**, the **endpoint in monospace**, the **right-aligned request count** with thousands separators, a **green "Active" status pill**, and **borderless gray edit/delete icons**.
4. Rows are separated by subtle dividers, have comfortable height, and highlight on hover.
5. The footer shows **"Hiển thị X–Y của N API"** on the left and a **numbered pager** on the right, with the active page filled and a next control.

### Acceptance Scenarios

| # | Given | When | Then |
|---|-------|------|------|
| 1 | The API screen is open | The user views the table | It matches the sample: light header, the seven columns in order, clean row dividers |
| 2 | The user views a row | They read each cell | ID is a blue link, method is plain text, endpoint is monospace, requests are right-aligned with separators, status is a green pill, actions are gray icons |
| 3 | The user hovers a row | The pointer is over it | The row shows a subtle hover highlight |
| 4 | There is more than one page | The user views the footer | The summary "Hiển thị X–Y của N API" shows on the left and a boxed numbered pager on the right, the active page filled |
| 5 | The user clicks a page number or the next control | Paging changes | The visible rows update and the active page indicator moves |
| 6 | An API is inactive | The user views its status | The status pill reflects the inactive state (distinct from the green active pill) |

### Edge Cases

- **Long endpoint or API name** → truncates/wraps gracefully without breaking row alignment.
- **No results** → an explicit empty state shows instead of an empty table.
- **Single page** → the pager shows one page; the summary range is still correct.
- **Large request counts** → displayed with thousands separators and stay right-aligned.

---

## Functional Requirements *(mandatory)*

Each requirement must be testable and unambiguous.

- **FR-1**: The table header MUST have a light background with small, muted labels, in column order: API ID, Tên API, Method, Endpoint, Requests, Trạng thái, Actions.
- **FR-2**: The API ID MUST render as a blue link.
- **FR-3**: The method MUST render as **plain text** (matching the sample — not a coloured pill).
- **FR-4**: The endpoint MUST render in a monospace, muted style.
- **FR-5**: The request count MUST be right-aligned and formatted with thousands separators.
- **FR-6**: The status MUST render as a small pill — green for active, and a visibly distinct style for inactive.
- **FR-7**: Row actions (edit, delete) MUST render as borderless gray icon buttons that darken on hover.
- **FR-8**: Rows MUST have subtle dividers, comfortable height, and a hover highlight; overall spacing MUST read clean/airy like the sample.
- **FR-9**: The footer MUST show "Hiển thị X–Y của N API" on the left and a numbered pager on the right; pager page buttons MUST be bordered boxes with the active page filled, plus a next control.

---

## Key Entities *(include if data is involved)*

| Entity | Maps to table | Notes |
|--------|---------------|-------|
| API Definition | `Api` | `Id` (linked "API ID"), `Name`, `Method`, `Endpoint`, `IsActive` (status pill). |
| — request count | *(derived)* | The "Requests" column is a derived/sample count, not a stored column. |

---

## Success Criteria *(mandatory)*

Measurable, technology-agnostic outcomes.

- **SC-1**: The rendered table visually matches the sample (header shading, column order, row dividers, spacing).
- **SC-2**: Method appears as plain text; status appears as a green pill for active rows.
- **SC-3**: Request counts are right-aligned with thousands separators across all rows.
- **SC-4**: The footer summary and the boxed numbered pager appear as in the sample, and paging works.
- **SC-5**: Row hover highlight is visible and consistent across rows.
- **SC-6**: All existing table behaviour (search, status filter, paging, edit/delete) continues to work unchanged.

---

## Scope

**In scope**:
- Visual styling of the API management table and its footer/pager to match the sample (header, cells, row dividers, hover, action icons, numbered boxed pager + summary).
- Presenting method as plain text and status as a pill, per the sample.

**Out of scope**:
- The table's data source and behaviour (search, status filter, paging, add/edit/delete) — unchanged from `specs/002`/`specs/008`.
- Other screens' tables (this change is scoped to the API management table; shared table styling may be reused but is not required to change other screens).
- Backend or schema changes.

---

## Assumptions

- "Match the sample" refers to the provided API-table reference image; exact spacing/colour tokens are taken from the project's design tokens at `/implement`.
- Method is shown as plain text (the sample does not show a coloured method pill); the method colour-badge used previously is replaced by plain text on this screen.
- The status pill uses the project's status colours (green for active) defined in the design system.
- The numbered pager keeps its existing compact behaviour (first/last/around-current with ellipsis) and only its button styling (bordered boxes, active filled) is aligned to the sample.

---

## Dependencies

- **`specs/008-shared-button-apis-page`** — the current API table and pager being refined.
- **`specs/009-typography-icon-system`** — the Inter type and Lucide icons used in the table.
- The design-system colours (status pill, links, borders) applied app-wide.
- The approved API-table reference image as the visual contract.
