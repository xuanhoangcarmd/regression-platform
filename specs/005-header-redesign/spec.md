# Feature Specification: Application Header Redesign

**Feature Directory**: `specs/005-header-redesign`
**Created**: 2026-06-21
**Status**: Draft

> Focus on **WHAT** and **WHY**, not HOW. Written for stakeholders, not implementers.
> Remove any section that does not apply (do not leave "N/A").

---

## Summary

Rework the top header (app bar) to match the approved reference: a **dark branded block** on the
far left (logo + "Regression Platform", aligned above the sidebar), the **current page/section
title** next to it, the **active-project switcher**, and right-aligned **utility controls** (a
light/dark theme toggle and a notifications bell with an unread indicator), all on a light bar with
a bottom divider. The goal is purely visual/layout fidelity so the header looks polished and
on-brand; navigation behaviour is unchanged.

---

## User Scenarios & Testing *(mandatory)*

### Primary User Flow

1. A user opens the application and sees the header: a dark brand block (logo + product name) at the far left, aligned with the sidebar.
2. Next to the brand, the header shows the **current page/section title** (e.g. "Dashboard").
3. The header shows the **active project** in a switcher control (project icon + name + dropdown affordance).
4. On the right, the header shows a **theme toggle** and a **notifications** control; the notifications control shows an unread indicator when there are unread items.
5. As the user navigates between sections, the page title in the header updates to the current area; the project switcher and utility controls stay in place.

### Acceptance Scenarios

| # | Given | When | Then |
|---|-------|------|------|
| 1 | The app is open | The user views the header | The brand block (left), page title, project switcher, and right-aligned theme + notification controls are all visible in that order |
| 2 | The user is on a section | They read the header title | The title matches the current area's name |
| 3 | The user opens the project switcher | They pick another project | The switcher reflects the newly active project (consistent with existing switch behaviour) |
| 4 | There are unread notifications | The user views the bell control | An unread indicator (badge/dot) is shown on it |
| 5 | The user navigates to a different section | The route changes | The header title updates while the rest of the header stays fixed |
| 6 | The viewport is narrowed | The user views the header | The header remains usable and its controls stay accessible without overflow |

### Edge Cases

- **Long page title or project name** → truncates gracefully without breaking the header layout.
- **No unread notifications** → the bell shows no unread indicator.
- **Narrow / mobile width** → non-essential elements may collapse, but brand, title, and project switcher remain reachable.
- **Active project changes from elsewhere** (e.g. sidebar) → the header switcher stays in sync.

---

## Functional Requirements *(mandatory)*

Each requirement must be testable and unambiguous.

- **FR-1**: The header MUST display the product brand (logo + "Regression Platform") in a dark block at the far left, visually aligned with the sidebar and consistent with the sidebar's brand styling.
- **FR-2**: The header MUST display the current page/section title, updated to reflect the active area as the user navigates.
- **FR-3**: The header MUST include the active-project switcher (project icon + name + dropdown affordance) that changes the active project, consistent with existing switch behaviour.
- **FR-4**: The header MUST include a light/dark theme toggle control.
- **FR-5**: The header MUST include a notifications control that displays an unread indicator when unread items exist and no indicator when there are none.
- **FR-6**: The header MUST follow the reference layout — brand (left), page title, project switcher, then right-aligned utility controls — on a light bar with a clear bottom divider.
- **FR-7**: The header MUST appear consistently on every screen and remain usable and non-overflowing across common desktop and tablet widths.
- **FR-8**: The active-project shown in the header MUST stay in sync with the active project shown elsewhere (e.g. the sidebar).

---

## Key Entities *(include if data is involved)*

| Entity | Maps to table | Notes |
|--------|---------------|-------|
| Project (active) | *(Planned)* | Shown in the switcher; multi-project is simulated (no `Project` table) — see `specs/003-project-scoped-navigation`. |
| Notification | *(Planned)* | No notifications table in the schema; the unread indicator is driven by a simulated count. |

---

## Success Criteria *(mandatory)*

Measurable, technology-agnostic outcomes.

- **SC-1**: The rendered header visually matches the reference in element order, the dark brand block, and right-aligned utility controls.
- **SC-2**: The header title always reflects the current section and changes on navigation.
- **SC-3**: The project switcher in the header and the active project elsewhere always show the same project.
- **SC-4**: The notifications control shows an unread indicator only when unread items exist.
- **SC-5**: The header renders without overflow and stays usable at desktop and tablet widths; long titles/names truncate rather than wrap or clip the layout.

---

## Scope

**In scope**:
- Header layout and styling to match the reference: dark brand block, current page title, project switcher placement, and right-aligned theme + notification controls on a light, divided bar.
- The notifications **unread indicator** (driven by a simulated count) and the presence of the theme-toggle control.
- Keeping the header title in sync with the current route and the switcher in sync with the active project.

**Out of scope**:
- Full application dark-theme switching behind the theme toggle (the control's presence is in scope; switching the whole app theme is a separate effort) — **(Planned)**.
- A notifications feed/panel, marking items read, or real notification data — **(Planned)**.
- A user/account menu or avatar (not part of this reference crop).
- Restyling the sidebar or content area (covered by other specs).

---

## Assumptions

- "Match the sample" means the header's layout, brand block, and control placement match the reference image; exact colour/spacing tokens are chosen at `/implement` to match it.
- The theme toggle and notifications are delivered as **styled controls in the correct position**; their full behaviour (switching the app theme, opening a notifications feed) is **(Planned)** and out of scope here.
- The notifications unread indicator uses a **simulated** count (no notifications exist in the schema).
- The page/section title is derived from the current navigation area (the same labels used in the navigation).
- The brand block reuses the product name and logo treatment already introduced in the sidebar (`specs/004-sidebar-menu-styling`).

---

## Dependencies

- **`specs/001-frontend-foundation`** — the application shell that hosts the header.
- **`specs/003-project-scoped-navigation`** — the active-project concept and switcher shown in the header.
- **`specs/004-sidebar-menu-styling`** — the brand styling the header's brand block aligns with.
- The approved reference image (header crop) as the visual contract.
