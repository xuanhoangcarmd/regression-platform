# Feature Specification: Sidebar Alignment & Collapse Toggle

**Feature Directory**: `specs/006-sidebar-align-toggle`
**Created**: 2026-06-21
**Status**: Draft

> Focus on **WHAT** and **WHY**, not HOW. Written for stakeholders, not implementers.
> Remove any section that does not apply (do not leave "N/A").

---

## Summary

Fix the left-column alignment and restore the menu toggle. Today the brand block (top-left) and
the navigation sidebar below it don't share the same width, so the dark left column looks
misaligned. This feature makes the **brand block and the sidebar one continuous dark column** of
identical width with aligned edges, and **re-adds the open/close (collapse) menu control** that
existed before, so users can hide/show the sidebar to free up space.

---

## User Scenarios & Testing *(mandatory)*

### Primary User Flow

1. A user opens the application and sees the dark left column: the brand block on top and the navigation directly below it, sharing the **same width and aligned edges** (one continuous column).
2. The user sees a **menu toggle control** (open/close icon) in the shell.
3. The user activates the toggle; the sidebar **collapses (hides)** and the content area expands to use the freed space.
4. The user activates the toggle again; the sidebar **expands (shows)** and the layout returns to the unified-column design.
5. The user navigates between sections; the chosen open/closed state is preserved.

### Acceptance Scenarios

| # | Given | When | Then |
|---|-------|------|------|
| 1 | The app is open | The user views the left column | The brand block and the sidebar have the same width with aligned left/right edges (no visible offset) |
| 2 | The sidebar is expanded | The user activates the menu toggle | The sidebar collapses/hides and the content area reflows to fill the space |
| 3 | The sidebar is collapsed | The user activates the menu toggle | The sidebar expands and the unified column is shown again |
| 4 | The user has set an open/closed state | They navigate to another section | The same open/closed state is retained |
| 5 | The viewport is tablet width | The user views and toggles the sidebar | The toggle works and the layout stays usable |

### Edge Cases

- **Collapsed state on load / after navigation** → the toggle still works and the brand/header remain visible and usable.
- **Narrow viewport** → toggling does not break the header or content layout.
- **Rapid toggling** → the layout settles correctly in the final state.

---

## Functional Requirements *(mandatory)*

Each requirement must be testable and unambiguous.

- **FR-1**: The brand block and the navigation sidebar MUST form a single continuous dark column with identical width and aligned left/right edges (no width mismatch between the logo area and the menu).
- **FR-2**: The shell MUST provide a menu toggle control (open/close icon), restored as it was before.
- **FR-3**: Activating the toggle MUST collapse (hide) the sidebar; activating it again MUST expand (show) it.
- **FR-4**: When the sidebar collapses or expands, the content area MUST reflow to use the available width.
- **FR-5**: The current open/closed state MUST be visually clear and persist while navigating within the session.
- **FR-6**: The brand block and header MUST remain visible and usable in both the collapsed and expanded states.

---

## Success Criteria *(mandatory)*

Measurable, technology-agnostic outcomes.

- **SC-1**: The logo block and the menu share the same width with aligned edges — no visible horizontal offset between them.
- **SC-2**: The menu toggle is present, and collapsing/expanding the sidebar works with the content reflowing accordingly.
- **SC-3**: The open/closed state is retained across navigation within a session.
- **SC-4**: Alignment and toggling behave correctly at both desktop and tablet widths.

---

## Scope

**In scope**:
- Aligning the brand block and sidebar into one continuous, equal-width dark column.
- Restoring the open/close menu toggle control and its collapse/expand behaviour, with content reflow.
- Preserving the open/closed state during a session.

**Out of scope**:
- Changing the navigation structure, colours, or item styling (covered by `specs/003`/`specs/004`).
- Header content/controls (covered by `specs/005`).
- Persisting the open/closed preference beyond the current session.

---

## Assumptions

- "Collapse" means hide/show the whole sidebar (as the toggle behaved before `specs/005`); a partial icon-only rail is not required.
- The unified column uses the existing sidebar width; exact toggle icon and placement (a hamburger-style control near the brand/left, as before) are chosen at `/implement`.
- Open/closed state is kept in memory for the session and resets on reload.

---

## Dependencies

- **`specs/001-frontend-foundation`** — the application shell.
- **`specs/004-sidebar-menu-styling`** — the dark sidebar being aligned.
- **`specs/005-header-redesign`** — the header/brand block whose width must match the sidebar.
- The approved reference image (showing the desired aligned column).
