# Feature Specification: Project-Scoped Navigation

**Feature Directory**: `specs/003-project-scoped-navigation`
**Created**: 2026-06-21
**Status**: Draft

> Focus on **WHAT** and **WHY**, not HOW. Written for stakeholders, not implementers.
> Remove any section that does not apply (do not leave "N/A").

---

## Summary

Restructure the application navigation from a single flat, global list into a **two-level,
project-centric** model: a few global entries (a cross-project overview and Settings) plus a
**Projects** group that lists each project and, under a selected project, that project's
functional sections (APIs, Request Pool, Runs, Compare, Reports, Ignore Rules, Configurations).
A visible **active-project switcher** keeps every functional screen scoped to one project at a
time. This matches how teams actually work — each business system (O'Reilly, AutoZone, CARiD,
Innova) is managed independently — and prevents data from different projects being mixed.

> **(Planned capability.)** Multi-project does not yet exist in the schema — there is no `Project`
> table and `Api`/`Run`/etc. have no `ProjectId` (see `docs/architecture/ImplementationStatus.md`).
> This feature is delivered against **simulated project data** (continuing `specs/002-ui-screens-mock-data`)
> and gives backend work a concrete contract. Going live later requires a schema migration.

---

## User Scenarios & Testing *(mandatory)*

### Primary User Flow

1. A user opens the application and sees the navigation with global entries (cross-project **Dashboard**, **Settings**) and a **Projects** group.
2. The Projects group lists the available projects; one is the **active project** by default.
3. The user expands a project and sees its scoped sections: **APIs, Request Pool, Runs, Compare, Reports, Ignore Rules, Configurations**.
4. The user selects a section; the corresponding screen opens showing **only that project's data**, and both the active project and the active section are indicated.
5. The user changes the active project via the project switcher; the current section re-scopes to the newly selected project.
6. The user opens (or bookmarks) a project-scoped link directly; the app restores both the active project and the section.

### Acceptance Scenarios

| # | Given | When | Then |
|---|-------|------|------|
| 1 | The app is running | The user opens it | The navigation shows global entries plus a Projects group with a default active project |
| 2 | A project is active | The user expands it in the navigation | Its scoped sections (APIs, Request Pool, Runs, Compare, Reports, Ignore Rules, Configurations) are listed |
| 3 | The user is on a project's APIs screen | They view the data | Only APIs belonging to the active project are shown |
| 4 | The user is on project A's Runs screen | They switch the active project to B | The Runs screen re-scopes to project B's runs without leaving the Runs section |
| 5 | A user has a project-scoped link | They open it directly | The app restores that project as active and opens the linked section |
| 6 | The active project is set | The user navigates between several sections | The same project stays active across the session |
| 7 | A link references a project that does not exist | The user opens it | A not-found state is shown with a way back to a valid project |
| 8 | The user opens the global Dashboard | They view it | It aggregates across all projects; the per-project Dashboard shows only the active project |

### Edge Cases

- **No projects exist** → the Projects group shows an empty state; functional sections are unavailable until a project exists.
- **Single project** → navigation still works; that project is active by default.
- **Project with no APIs / requests / runs** → its sections open and show their own empty states (not an error).
- **Switching project while on a detail screen** (e.g. a specific run that belongs to the previous project) → the app returns to the list level of that section for the new project rather than showing another project's record.
- **Unknown project reference** in a link → not-found, with navigation back to a valid project.
- **(Planned) areas** reached from navigation → show the reserved/placeholder state, scoped under the project where applicable.

---

## Functional Requirements *(mandatory)*

Each requirement must be testable and unambiguous.

- **FR-1**: The navigation MUST present a two-level structure: global entries (a cross-project Dashboard and Settings) and a **Projects** group.
- **FR-2**: The Projects group MUST list the available projects, and each project MUST expand to its scoped sections: APIs, Request Pool, Runs, Compare, Reports, Ignore Rules, and Configurations.
- **FR-3**: The application MUST maintain one **active project** at a time and expose a clearly visible switcher to change it.
- **FR-4**: Selecting a section under a project MUST open that section scoped to the project, and the navigation MUST indicate both the active project and the active section.
- **FR-5**: Every project-scoped screen (APIs, Request Pool, Runs, Compare, Reports, Ignore Rules, Configurations) MUST display only data belonging to the active project.
- **FR-6**: Changing the active project MUST re-scope the current section to the newly selected project, keeping the user on the same section type where one exists.
- **FR-7**: A project-scoped location MUST be directly addressable so that opening it restores both the active project and the section.
- **FR-8**: The system MUST handle a reference to a non-existent project with a not-found state and a path back to a valid project.
- **FR-9**: The global Dashboard MUST aggregate metrics across all projects; the per-project Dashboard MUST show only the active project's metrics.
- **FR-10**: The active project MUST persist as the user moves between sections within a session.
- **FR-11**: Settings MUST remain a global (non-project-scoped) area.
- **FR-12**: Configurations MUST be a per-project grouping that contains the project's Ignore Rules (and is the home for future per-project configuration).

---

## Key Entities *(include if data is involved)*

All multi-project concepts below are **(Planned)** — represented by simulated data in this feature.
See `docs/database/Tables.md` and `docs/architecture/ImplementationStatus.md`.

| Entity | Maps to table | Notes |
|--------|---------------|-------|
| Project | *(Planned)* | No `Project` table exists; examples: O'Reilly, AutoZone, CARiD, Innova. Owns APIs, Requests, Runs, Compare Results. |
| API Definition | `Api` | **(Planned association)** — `Api` has no `ProjectId`; project ownership is simulated. |
| Test Request | `ApiRequest` | Belongs to its API's project (simulated). |
| Run | `Run` | **(Planned association)** — no `ProjectId`; project scope simulated. |
| Compare Result | `CompareResult` | Scoped to the project of the runs compared (simulated). |
| Ignore Rule | `CompareIgnoreRule` | Shown under the project's Configurations; current schema has no project scope. |
| Environment (DEV/QA/UAT/PROD) | *(Planned)* | Per `ProjectManagement.md`; `Run` has no `Environment` column. |

---

## Success Criteria *(mandatory)*

Measurable, technology-agnostic outcomes.

- **SC-1**: From the navigation, a user can pick any listed project and reach each of its seven scoped sections, each showing only that project's data.
- **SC-2**: Changing the active project updates the visible data on the current section to the new project within one interaction, without manual re-navigation.
- **SC-3**: Opening a project-scoped link directly lands on the correct project and section every time.
- **SC-4**: The global Dashboard shows cross-project totals while a project Dashboard shows a strict subset for that project (totals reconcile).
- **SC-5**: Selecting two different projects in turn never shows one project's records on the other's screens (no cross-project leakage).
- **SC-6**: An invalid project reference always resolves to a not-found state, never a blank or mixed screen.

---

## Scope

**In scope**:
- Two-level, project-scoped navigation (global entries + Projects group with per-project sections).
- An active-project concept with a visible switcher and session persistence.
- Project-scoped addressing of sections so links restore project + section.
- Re-scoping the existing screens (from `002`) to the active project using simulated project data.
- Global vs per-project Dashboard distinction; Settings as a global area; Configurations as a per-project group housing Ignore Rules.

**Out of scope**:
- Any schema change or backend work to make projects real (this is simulated; going live needs a `Project` table and `ProjectId` associations — a separate effort).
- Project create/edit/delete management screens (only selection/scoping here).
- Authentication, authorization, and per-project access control.
- Environments (DEV/QA/UAT/PROD) behaviour beyond display; AI Analysis screens.
- Final branding/theme (e.g. dark mode toggle) shown in the reference image.

---

## Assumptions

- This feature continues the **simulated-data** approach of `specs/002-ui-screens-mock-data`; projects and their ownership are mock data, flagged **(Planned)**, shaped so a real backend can replace them.
- The seven per-project sections mirror the reference mockup: APIs, Request Pool, Runs, Compare, Reports, Ignore Rules, Configurations. **Run History** is folded into **Runs**; **AI Analysis** is not part of the per-project menu (remains a reserved/global Planned area).
- A default active project is selected on first load (the first listed project).
- The cross-project (global) Dashboard is retained in addition to each project's own Dashboard.
- The reference image's top-bar elements (date range, dark mode, notifications, user menu) are not required by this feature except the **project switcher**, which is required.

---

## Dependencies

- **`specs/001-frontend-foundation`** — the shell and navigation this feature restructures.
- **`specs/002-ui-screens-mock-data`** — the screens and simulated-data layer that become project-scoped.
- `docs/business/ProjectManagement.md` and `docs/architecture/ImplementationStatus.md` — the (Planned) multi-project rules and the implemented-vs-planned boundary.
