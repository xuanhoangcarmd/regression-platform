# Feature Specification: UI Screens with Simulated Data

**Feature Directory**: `specs/002-ui-screens-mock-data`
**Created**: 2026-06-21
**Status**: Draft

> Focus on **WHAT** and **WHY**, not HOW. Written for stakeholders, not implementers.
> Remove any section that does not apply (do not leave "N/A").

---

## Summary

Build the full set of primary screens shown in the approved UI mockup — Dashboard, API
management, Request Pool, Create Run, Run Detail, Compare setup, Compare result overview,
Per-request Compare detail, and Ignore Rules — and populate every screen with realistic
**sample (simulated) data** so the entire product can be demonstrated, reviewed, and validated
**without a live backend**. This lets stakeholders sign off on layout, content, and interaction
flows early, and gives backend work a concrete UI contract to build against later.

---

## User Scenarios & Testing *(mandatory)*

### Primary User Flow

1. A reviewer opens the application and lands on the **Dashboard**, which shows summary metrics, result charts, and a recent-runs list populated with sample data.
2. They open **API management** and browse a paginated, searchable list of API definitions.
3. They open **Request Pool** and browse the reusable test requests, filtered by API.
4. They open **Create Run**, fill in the run form, and submit; the new run appears in the runs data for the session.
5. They open a **Run Detail** screen and review per-run metrics and per-API pass/fail results across tabs.
6. They open **Compare**, pick a baseline run (Run A) and a target run (Run B), set options, and start a comparison.
7. They review the **Compare result overview** (added / removed / changed / unchanged counts per API) and drill into a single request's **Compare detail** to see a side-by-side response diff with field-level changes highlighted.
8. They open **Ignore Rules** and review/add/edit/remove JSON-path rules used during comparison.

### Acceptance Scenarios

| # | Given | When | Then |
|---|-------|------|------|
| 1 | The app is running with no backend available | The reviewer opens any of the nine screens | The screen renders fully with representative sample data and no error |
| 2 | The API list has more items than one page | The reviewer changes page or searches/filters | The list updates to show the matching/paged subset |
| 3 | The Create Run form is shown | The reviewer submits a valid run | A confirmation is shown and the new run appears in the runs list for the current session |
| 4 | The Create Run form is shown | The reviewer submits with a required field empty | Submission is blocked and the invalid field is indicated |
| 5 | Two runs are selected for comparison | The reviewer starts the comparison | The result overview shows summary counts and a per-API breakdown |
| 6 | A per-request compare detail is open | The reviewer views the Response/Diff tab | Modified, added, and removed fields are visually distinguished and summarized |
| 7 | The Ignore Rules screen is shown | The reviewer adds or removes a rule | The rules list reflects the change for the current session |
| 8 | Any list screen has an active search/filter with no matches | The reviewer applies it | A clear empty-state message is shown instead of an empty grid |

### Edge Cases

- **Empty data sets** (no APIs, no requests, no runs, no rules) → each screen shows an empty-state message, not a broken layout.
- **Same run selected for both sides** of a comparison (Run A = Run B) → the reviewer is prevented from starting, or the result clearly shows zero differences.
- **Compare with no differences** → the result overview and detail show an explicit "no changes" outcome.
- **Large volumes** (e.g. thousands of requests) → list screens remain responsive via paging; counts display with thousands separators.
- **Large or deeply-nested response bodies** in the diff viewer → content scrolls within its panel without breaking the page layout.
- **Session reset** → because data is simulated, edits/creations made during a session reset on reload; this is expected and must not appear as data loss errors.
- **Planned areas** (e.g. AI Analysis) reached from navigation → show the reserved/placeholder state from the foundation, not a sample screen.

---

## Functional Requirements *(mandatory)*

Each requirement must be testable and unambiguous.

### Data & general behavior

- **FR-1**: The system MUST present every screen populated with representative sample data and MUST function without any live backend connection.
- **FR-2**: Sample data across screens MUST be internally consistent — runs reference real requests, requests reference real APIs, and compare results reference the two runs being compared.
- **FR-3**: Create, edit, and delete actions MUST operate on the simulated data so the lists reflect the change for the current session (changes need not survive a reload).
- **FR-4**: All list screens MUST support pagination, free-text search, and the filters shown in the mockup, and MUST show an empty-state message when no items match.
- **FR-5**: Numeric metrics MUST be formatted for readability (thousands separators) and percentages shown to one decimal place where the mockup does.
- **FR-6**: Every screen MUST be reachable through the existing application navigation, and the active area MUST be indicated.

### Dashboard

- **FR-7**: The Dashboard MUST show summary metric cards (run count, API count, request count, comparison count) each with a period-over-period change indicator.
- **FR-8**: The Dashboard MUST show result-distribution charts for recent runs (passed / failed / warning / in-progress) and recent comparisons (no-change / changed / failed / new), and a recent-runs list.

### API management

- **FR-9**: The API screen MUST list API definitions with identifier, name, HTTP method, endpoint, request count, and status, with row actions to add, edit, and remove.

### Request Pool

- **FR-10**: The Request Pool screen MUST list test requests with identifier, owning API, title, HTTP method, and status, filterable by API, with add/edit/remove actions.

### Create Run

- **FR-11**: The Create Run screen MUST provide a form to start a run with run name, run type, target environment, description, and an API-selection choice ("all APIs" or "specific APIs"), validating required fields before submission.

### Run Detail

- **FR-12**: The Run Detail screen MUST show the run's status and metadata, summary metrics (total / passed / failed / warning / duration), and a per-API result breakdown including a pass-rate indicator, organized across the tabs shown in the mockup (Overview, APIs, Requests, Results, Logs).

### Compare

- **FR-13**: The Compare setup screen MUST let the reviewer select a baseline run (Run A) and a target run (Run B) with their metadata, choose a comparison scope (all requests or specific APIs), and toggle options for ignoring array order and applying ignore rules.
- **FR-14**: The Compare result overview MUST show summary counts (total, unchanged, changed, added, removed) and a per-API breakdown with an unchanged-ratio indicator, plus actions to export and share the report.
- **FR-15**: The Per-request Compare detail MUST show a breadcrumb of the comparison context, a side-by-side view of the two responses, and a field-level diff summary that distinguishes modified, added, and removed fields (referenced by JSON path with old → new values).

### Ignore Rules

- **FR-16**: The Ignore Rules screen MUST list configured rules (identifier, JSON path, description, applicability, status) with add/edit/remove actions, and MUST display the JSON-path format guidance shown in the mockup.

---

## Key Entities *(include if data is involved)*

Mapped to the real `RegressionDB` schema (see `docs/database/Tables.md`). Concepts not yet in the
schema are flagged **(Planned)** per `docs/architecture/ImplementationStatus.md`. In this feature
all entities are represented by **simulated data only** — no persisted records are created.

| Entity | Maps to table | Notes |
|--------|---------------|-------|
| API Definition | `Api` | name, method, endpoint, status; "request count" is a derived/sample figure |
| Test Request | `ApiRequest` | title, owning `ApiId`, method (via its API), status |
| Run | `Run` | title, status, timing, total requests |
| Run Response / Result | `RunResponse` | per-request status, duration, response body/hash — drives Run Detail and the diff |
| Compare Job | `CompareJob` | run A vs run B, status/progress for an async comparison |
| Compare Result | `CompareResult` | summary counts (pass/changed/failed/added/removed/modified) for Run A vs Run B |
| Compare Detail | `CompareDetail` | per-request `Result` + field-level `DiffJson` shown in the diff viewer |
| Ignore Rule | `CompareIgnoreRule` | `JsonPath`, ignore-order, enabled, description |
| Dashboard metrics | *(derived)* | aggregations over the above; no dedicated table |
| Run type ("Sprint") | *(Planned)* | `Run` has no type column; treated as a sample/display attribute only |
| Run environment ("Staging") | *(Planned)* | `Run` has no `Environment` column |
| AI Analysis | *(Planned)* | no `AIAnalysis` table; not shown as a sample screen |
| Users | *(Planned)* | no user table in the current schema |
| Project scope | *(Planned)* | no `Project` table; data is not project-scoped |

---

## Success Criteria *(mandatory)*

Measurable, technology-agnostic outcomes.

- **SC-1**: All nine mockup screens render completely with sample data while disconnected from any backend.
- **SC-2**: Each list screen (APIs, Request Pool, Run per-API results, Compare per-API results, Ignore Rules) supports paging, search/filter, and shows an empty state when nothing matches.
- **SC-3**: Submitting a valid Create Run form adds a run that is then visible in the runs data; submitting an invalid form is blocked with the offending field indicated.
- **SC-4**: Starting a comparison from two selected runs produces an overview with summary counts and a per-API breakdown that drills down to a per-request diff.
- **SC-5**: In the per-request diff, modified / added / removed fields are visually distinguishable and counted in a summary that matches the highlighted changes.
- **SC-6**: A reviewer can reach every screen via navigation and complete the end-to-end primary flow without encountering a backend error.

---

## Scope

**In scope**:
- The nine screens depicted in the mockup, with the layout, content, and interactions shown.
- Realistic, internally-consistent sample data powering all screens.
- List behaviors (paging, search, filters, empty states), form validation, and session-scoped create/edit/delete on the sample data.
- Navigation between screens using the existing application shell.

**Out of scope**:
- Any connection to, or dependency on, a live backend or real APIs.
- Persisting data beyond the current session.
- Detailed Reports, Users, and Settings screens (their navigation entries remain placeholders).
- AI Analysis, multi-project scoping, authentication/authorization, and final branding/theme work.
- Real export/share file generation (the actions are present but produce sample output only).

---

## Assumptions

- "Create the API and simulated data, no backend yet" is interpreted as: provide the screens' data through a **swappable data source backed by sample data**, shaped to match the real schema, so a real backend can replace it later without changing the screens. (Detailed approach is decided at `/implement` time.)
- CRUD actions act on the sample data **within the current session** and reset on reload — acceptable for a demo/mock and not treated as data loss.
- Sample data uses domain-appropriate values consistent with the mockup (vehicle fitment, DTC codes, sprint-style run names) but exact values need not match the mockup pixel-for-pixel.
- Display identifiers (e.g. `API-001`, `REQ-00001`, `RUN-28`, `RUL-001`) are formatted presentations of the underlying numeric keys.
- Run type ("Sprint") and environment ("Staging") are shown as sample/display attributes only, since the schema does not yet store them.
- Reports / Users / Settings remain as reserved navigation entries from the foundation feature.

---

## Dependencies

- **`specs/001-frontend-foundation`** — the application shell, navigation, routing skeleton, and placeholder/not-found behavior this feature builds its screens into.
- The approved UI mockup (the nine-screen reference image) as the visual and interaction contract.
- The real `RegressionDB` schema (`docs/database/Tables.md`) as the shape the sample data mirrors.
