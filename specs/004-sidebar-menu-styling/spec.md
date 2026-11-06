# Feature Specification: Sidebar Menu Styling (Dark Theme)

**Feature Directory**: `specs/004-sidebar-menu-styling`
**Created**: 2026-06-21
**Status**: Draft

> Focus on **WHAT** and **WHY**, not HOW. Written for stakeholders, not implementers.
> Remove any section that does not apply (do not leave "N/A").

---

## Summary

Restyle the navigation sidebar to match the approved reference image: a **dark navy/slate panel
with light text**, a branded header, an expandable **Projects** group, a clearly highlighted
**active project**, and a **blue-accented selected section**. The goal is purely visual fidelity —
the navigation's structure and behaviour (from `specs/003-project-scoped-navigation`) stay the
same; only its colour, size, and styling change so the product looks polished and on-brand.

---

## User Scenarios & Testing *(mandatory)*

### Primary User Flow

1. A user opens the application and sees the sidebar rendered as a **dark** panel with a branded header (logo + product name) at the top.
2. Top-level entries (Dashboard, Projects, Settings) appear as light-text rows with a leading icon; expandable groups show a chevron indicating expanded/collapsed state.
3. The active project is shown as a highlighted, expanded container revealing its sections, indented beneath it.
4. The section the user is currently on is highlighted with a distinct blue accent and high-contrast text; all other items use the muted light-text style.
5. Hovering an item gives subtle feedback that is clearly different from the selected state.

### Acceptance Scenarios

| # | Given | When | Then |
|---|-------|------|------|
| 1 | The app is open | The user views the sidebar | The panel is dark, text is light, and the brand header is visible at the top |
| 2 | A project is active | The user views the Projects group | The active project is visually highlighted and expanded; collapsed projects show a collapsed chevron |
| 3 | The user is on a section | They look at that project's section list | Exactly one section (the current one) shows the blue selected style |
| 4 | The user hovers a non-selected item | The pointer is over it | The item shows a subtle hover style distinct from the selected style |
| 5 | The user navigates to another section | The route changes | The selected highlight moves to the new section and leaves the previous one |

### Edge Cases

- **Long project or section names** → truncate gracefully (ellipsis) without breaking the row or wrapping awkwardly.
- **Many projects** → the project list scrolls within the panel; the brand header and Settings remain reachable.
- **Collapsed vs expanded** projects → the chevron direction reflects state consistently.
- **Active project differs from expanded project** → the active project is the one highlighted; a user may still expand others.
- **Small/zoomed viewport** → item sizing, spacing, and icon alignment remain consistent and legible.

---

## Functional Requirements *(mandatory)*

Each requirement must be testable and unambiguous.

- **FR-1**: The sidebar MUST use a dark theme — a deep navy/slate background with light-coloured text and icons — matching the reference image.
- **FR-2**: A branded header (product logo + "Regression Platform") MUST appear at the top of the sidebar on the same dark background.
- **FR-3**: Top-level entries (Dashboard, Projects, Settings) MUST show a leading icon and label; expandable groups (Projects, each project) MUST show a chevron that reflects expanded/collapsed state.
- **FR-4**: The active project MUST be visually distinguished as a highlighted container and shown expanded, revealing its sections.
- **FR-5**: The currently selected section MUST be highlighted with a distinct blue accent background and high-contrast text, visibly different from all unselected items, and only one section may show this state at a time.
- **FR-6**: Project sections MUST be visually indented beneath their project to convey hierarchy.
- **FR-7**: A hover state MUST provide subtle feedback that is distinct from both the default and the selected states.
- **FR-8**: Navigation items MUST have consistent sizing, vertical spacing, rounded corners, and icon/label alignment, matching the compact rhythm of the mockup.
- **FR-9**: The sidebar MUST have a consistent fixed width consistent with the mockup proportions.
- **FR-10**: Settings MUST appear as a global entry separated toward the bottom of the panel.
- **FR-11**: Text and interactive elements MUST remain legible on the dark background (sufficient contrast).

---

## Success Criteria *(mandatory)*

Measurable, technology-agnostic outcomes.

- **SC-1**: The rendered sidebar visually matches the reference image in colour scheme, hierarchy, and the highlighted active project + selected section.
- **SC-2**: At any time, exactly one section shows the selected (blue) style; navigating moves it.
- **SC-3**: Body text on the sidebar background meets a contrast ratio of at least 4.5:1.
- **SC-4**: Default, hover, and selected states are visually distinguishable from one another.
- **SC-5**: Item height, spacing, indentation, and icon alignment are uniform across all rows.
- **SC-6**: Long names truncate with an ellipsis instead of overflowing or wrapping.

---

## Scope

**In scope**:
- Visual restyling of the navigation sidebar: dark palette, branded header, item states (default / hover / selected), expand/collapse affordance, indentation, sizing, and spacing.
- Active-project highlight and selected-section accent styling.
- Truncation and scroll behaviour of the project/section lists within the panel.

**Out of scope**:
- Any change to navigation structure, routes, or behaviour (delivered by `specs/003-project-scoped-navigation`).
- Restyling the top bar, content area, or other screens (the reference image covers only the sidebar); a full app-wide dark theme.
- New navigation entries or projects.
- Icon set changes beyond what each entry already uses.

---

## Assumptions

- Exact colour values and spacing tokens are chosen at `/implement` time to match the reference image (e.g. a deep navy/slate background, muted light text, and a blue selected accent); this spec fixes the *intent*, not the hex codes.
- The dark theme applies to the **sidebar only**; the top bar and content area are unchanged unless a later spec asks for a full dark theme.
- This builds on the two-level, project-scoped navigation from `specs/003-project-scoped-navigation`; only presentation changes.
- The branded header reuses the existing product name and logo treatment.

---

## Dependencies

- **`specs/003-project-scoped-navigation`** — the navigation structure (global entries, Projects group, per-project sections, active project) being restyled.
- **`specs/001-frontend-foundation`** — the application shell that hosts the sidebar.
- The approved reference image (dark sidebar) as the visual contract.
