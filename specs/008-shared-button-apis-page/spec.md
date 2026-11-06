# Feature Specification: Shared Button + API Management Page Polish

**Feature Directory**: `specs/008-shared-button-apis-page`
**Created**: 2026-06-21
**Status**: Draft

> Focus on **WHAT** and **WHY**, not HOW. Written for stakeholders, not implementers.
> Remove any section that does not apply (do not leave "N/A").

---

## Summary

Two related improvements: (1) refine the **API management** screen so it matches the approved
reference (clean card, title/subtitle, a top-right primary action, search + status filter, a tidy
table with linked IDs / method & status badges / right-aligned counts / row actions, and a
pagination summary + pager); and (2) introduce a **single reusable action button** used across the
whole application so every button looks and behaves the same. The API page's "+ Thêm API" button is
the first adopter, and existing action buttons elsewhere are switched to the same reusable button
for visual consistency.

---

## User Scenarios & Testing *(mandatory)*

### Primary User Flow

1. A user opens the **API management** screen and sees a card headed "APIs" with the subtitle "Danh sách API" and a **primary action button** ("+ Thêm API") at the top-right.
2. Above the table, the user sees a **search field** and a **status filter**.
3. The table lists APIs with: **API ID as a link**, API name, **method badge**, endpoint, **right-aligned request count**, **status badge**, and **row actions** (edit, delete) — matching the reference.
4. Below the table, a **pagination summary** ("Hiển thị X–Y của N API") and a **page navigator** are shown.
5. Across other screens, the user sees the **same button style** for equivalent actions (the buttons are visually identical everywhere).

### Acceptance Scenarios

| # | Given | When | Then |
|---|-------|------|------|
| 1 | The API screen is open | The user views the card | Title "APIs", subtitle "Danh sách API", and a top-right "+ Thêm API" primary button are shown |
| 2 | The API list has more items than one page | The user views the footer | A summary ("Hiển thị X–Y của N") and a page navigator are shown, and paging works |
| 3 | The user views a table row | They look at the cells | ID is a link, method and status are badges, request count is right-aligned, and edit/delete row actions are present |
| 4 | The user compares the "+ Thêm API" button with action buttons on other screens | They inspect both | The buttons share the same appearance and states (same reusable button) |
| 5 | A reusable button is in a loading/disabled state | The user tries to activate it | Activation is prevented and the state is visually indicated |
| 6 | The user searches or filters by status | They apply it | The table updates to the matching subset (resetting to the first page) |

### Edge Cases

- **No APIs / no matches** → the table shows an explicit empty state instead of an empty grid.
- **Long endpoint or API name** → truncates gracefully without breaking the row.
- **Single page of results** → the pager reflects one page; the summary still shows the correct range.
- **Button with no icon / icon-only button** → the reusable button still renders consistently.

---

## Functional Requirements *(mandatory)*

Each requirement must be testable and unambiguous.

### API management page

- **FR-1**: The API screen MUST present a card with the title "APIs" and subtitle "Danh sách API".
- **FR-2**: A primary action button labelled "+ Thêm API" MUST appear at the top-right of the card.
- **FR-3**: A search field and a status filter MUST appear above the table; applying either MUST filter the list and reset to the first page.
- **FR-4**: The table MUST show, in order: API ID (rendered as a link), API name, method (as a badge), endpoint, request count (right-aligned), status (as a badge), and row actions (edit and delete).
- **FR-5**: A pagination area MUST show a result summary ("Hiển thị X–Y của N API") and a page navigator that changes pages.
- **FR-6**: When there are no results, the screen MUST show an explicit empty state.
- **FR-7**: The page's visual styling (spacing, badges, alignment, linked IDs) MUST match the reference.

### Shared button (app-wide consistency)

- **FR-8**: The application MUST provide a **single reusable action button** that is the standard for action buttons throughout the app.
- **FR-9**: The reusable button MUST support a label, an optional leading icon, and visual **variants** (at least primary and secondary) plus a disabled/loading state that prevents repeated activation.
- **FR-10**: The API page's "+ Thêm API" button MUST use the reusable button.
- **FR-11**: Existing prominent action buttons on other screens (e.g. create/add, export, share) MUST be switched to the reusable button so all such buttons are visually consistent.
- **FR-12**: All instances of the reusable button MUST render identically for the same variant/state, regardless of screen.

---

## Key Entities *(include if data is involved)*

| Entity | Maps to table | Notes |
|--------|---------------|-------|
| API Definition | `Api` | `Id` (shown as the linked "API ID"), `Name`, `Method`, `Endpoint`, `IsActive` (status badge). |
| — request count | *(derived)* | The "Requests" column is a derived/sample count of test requests for the API, not a stored column. |

---

## Success Criteria *(mandatory)*

Measurable, technology-agnostic outcomes.

- **SC-1**: The API management screen visually matches the reference (header, primary action placement, table columns/badges/alignment, pagination summary + pager).
- **SC-2**: Search and status filtering update the table and reset to the first page; paging changes the visible rows; an empty state shows when nothing matches.
- **SC-3**: The "+ Thêm API" button and equivalent action buttons on other screens are the same reusable button and render identically for the same variant/state.
- **SC-4**: The reusable button's disabled/loading state visibly prevents repeated activation.
- **SC-5**: Changing the reusable button in one place changes it everywhere it is used (single source of truth).

---

## Scope

**In scope**:
- Refining the API management screen to match the reference (card, header + primary action, search/filter, table, pagination summary + pager, empty state).
- Introducing one reusable action button (with variants + disabled/loading state) as the app-wide standard.
- Adopting the reusable button on the API page and switching existing prominent action buttons on other screens to it.

**Out of scope**:
- Backend work or real data (the screen continues to use simulated, project-scoped data).
- Redesigning other screens beyond switching their action buttons to the reusable button.
- Form/dialog contents for adding/editing an API beyond opening them from the button (already covered by `specs/002`).
- Icon set or colour-token changes beyond what the button variants need.

---

## Assumptions

- "Match the UI" refers to the reference image of the API management screen; exact spacing/colour tokens are chosen at `/implement`.
- The reusable button consolidates the styling currently applied ad hoc per screen; row-level icon actions (edit/delete) may use an icon-button variant of the same reusable button.
- Full migration of every button app-wide is best-effort in this feature; the reusable button is the standard going forward and is applied to the prominent action buttons that exist today.
- The API screen keeps its existing behaviour (search, status filter, paging, add/edit/delete on simulated data) from `specs/002` and stays project-scoped per `specs/003`.

---

## Dependencies

- **`specs/002-ui-screens-mock-data`** — the existing API management screen and its simulated data/behaviour.
- **`specs/003-project-scoped-navigation`** — the API list is scoped to the active project.
- The existing shared UI components (badges, page header, empty state) the page already uses.
- The approved reference image of the API management screen.
