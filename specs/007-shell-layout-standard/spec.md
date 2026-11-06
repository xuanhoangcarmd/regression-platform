# Feature Specification: Standard Shell Layout (Sidebar + Header)

**Feature Directory**: `specs/007-shell-layout-standard`
**Created**: 2026-06-21
**Status**: Draft

> Focus on **WHAT** and **WHY**, not HOW. Written for stakeholders, not implementers.
> Remove any section that does not apply (do not leave "N/A").

---

## Summary

Correct the application shell layout so it reads as a standard admin layout. Today the header is a
full-width bar across the top while the brand sits **below** it inside the sidebar, producing two
stacked bars on the left, a misaligned brand, and an awkward inner scrollbar. The fix: a
**full-height sidebar on the left** (brand at its very top), and a **header bar that occupies only
the area to the right of the sidebar** (over the content). The brand at the top of the sidebar and
the header bar share the same top edge and height, so the top row reads as one clean line, and the
sidebar and content each scroll independently and cleanly.

---

## User Scenarios & Testing *(mandatory)*

### Primary User Flow

1. A user opens the application and sees a **full-height sidebar** on the left with the brand at its very top.
2. To the right of the sidebar, a **header bar** spans the content area only, containing the menu toggle, current page title, project switcher, and utility controls.
3. The brand (top of sidebar) and the header bar are on the **same top row**, aligned in height — no stacked bars, no vertical offset.
4. The user scrolls a long page; the **content scrolls independently** while the header stays put; the **sidebar scrolls on its own** only if its items overflow.
5. The user toggles the menu; the sidebar (with its brand) collapses and the header + content expand to fill the width.

### Acceptance Scenarios

| # | Given | When | Then |
|---|-------|------|------|
| 1 | The app is open | The user views the shell | The sidebar spans full height on the left with the brand at its top; the header sits only to the right of the sidebar |
| 2 | The app is open | The user looks at the top row | The brand and the header bar align on the same top edge with the same height (one clean line, no offset) |
| 3 | A page is taller than the viewport | The user scrolls | The content scrolls while the header remains fixed above it; the sidebar does not scroll with the content |
| 4 | The sidebar items exceed its height | The user views the sidebar | The sidebar scrolls within itself cleanly (single scrollbar), without spilling into the header or content |
| 5 | The sidebar is shown | The user activates the menu toggle | The sidebar collapses and the header + content reflow to full width |
| 6 | The viewport is tablet width | The user views the shell | The layout stays aligned and usable |

### Edge Cases

- **Long page title or project name** in the header → truncates rather than pushing controls off-screen.
- **Many navigation items** → only the sidebar scrolls; the brand stays pinned at the top of the sidebar.
- **Collapsed sidebar** → no leftover gap where the sidebar was; content uses the full width.
- **Short content** → no unnecessary scrollbars appear.

---

## Functional Requirements *(mandatory)*

Each requirement must be testable and unambiguous.

- **FR-1**: The sidebar MUST span the full viewport height on the left, with the brand pinned at its top.
- **FR-2**: The header bar MUST occupy only the region to the right of the sidebar (over the content), not the full window width.
- **FR-3**: The header MUST contain the menu toggle, the current page title, the project switcher, and the utility controls (theme, notifications).
- **FR-4**: The brand at the top of the sidebar and the header bar MUST share the same top edge and the same height, so the top row reads as a single aligned line.
- **FR-5**: Activating the menu toggle MUST collapse the sidebar (with its brand); the header and content MUST then reflow to use the full width, with no leftover gap.
- **FR-6**: When the sidebar's items overflow, the sidebar MUST scroll within itself with a single scrollbar, without overflowing into the header or content.
- **FR-7**: The content area MUST scroll independently while the header stays fixed above it; the sidebar MUST NOT scroll together with the content.
- **FR-8**: The sidebar, header, and content MUST align with no gaps or offsets between them, and remain consistent at desktop and tablet widths.

---

## Success Criteria *(mandatory)*

Measurable, technology-agnostic outcomes.

- **SC-1**: The sidebar is full height with the brand at top; the header is positioned only to the right of the sidebar (verified visually against the reference).
- **SC-2**: The brand and header align on the same top line with equal height — no visible vertical offset or stacked bars.
- **SC-3**: Scrolling a long page moves only the content; the header stays fixed and the sidebar does not move.
- **SC-4**: At most one scrollbar appears per scrollable region (sidebar, content); none appear when content fits.
- **SC-5**: Toggling the menu collapses/expands the sidebar with the content reflowing to full width and back, leaving no gap.
- **SC-6**: The layout holds (aligned, usable) at desktop and tablet widths.

---

## Scope

**In scope**:
- The shell layout structure: full-height left sidebar (brand at top), header bar confined to the content region, and independent scroll regions for sidebar and content.
- Correct alignment of the top row (brand + header) and removal of stacked-bar / double-scrollbar artifacts.
- Keeping the existing menu toggle (collapse/expand) behaviour within the corrected structure.

**Out of scope**:
- Navigation structure, items, or colours (covered by `specs/003`/`specs/004`).
- Header control set and styling beyond placement (covered by `specs/005`).
- The brand's own styling (covered by `specs/004`/`specs/006`).

---

## Assumptions

- "Make it standard" means the conventional admin layout: a full-height left sidebar with the brand at the top-left corner and a header bar over the content area only (matching the project's reference dashboard mockup).
- The brand and header heights are equal so their bottom edges align across the top row.
- The menu toggle continues to hide/show the whole sidebar (per `specs/006`); open/closed state persists for the session.
- Exact spacing/height tokens are chosen at `/implement` to match the reference.

---

## Dependencies

- **`specs/001-frontend-foundation`** — the shell being corrected.
- **`specs/004-sidebar-menu-styling`**, **`specs/005-header-redesign`**, **`specs/006-sidebar-align-toggle`** — the sidebar, header, and toggle this layout reorganises into the standard structure.
- The approved reference dashboard mockup (full-height sidebar + content-area header).
