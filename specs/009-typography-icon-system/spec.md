# Feature Specification: Typography & Icon Design System

**Feature Directory**: `specs/009-typography-icon-system`
**Created**: 2026-06-21
**Status**: Draft

> Focus on **WHAT** and **WHY**, not HOW. Written for stakeholders, not implementers.
> Remove any section that does not apply (do not leave "N/A").

---

## Summary

Adopt the approved design system from the reference sheet (`docs/rules/angular/font/font.png`) so
the whole application shares one typeface, one type scale, one text-colour palette, and one icon
style. Concretely: **Inter** as the single font (already applied), a consistent **heading/body type
scale**, the defined **text-colour palette**, and the **Lucide outline** icon set replacing the
current icons. The goal is a consistent, on-brand look where typography and icons are defined once
and reused everywhere.

---

## User Scenarios & Testing *(mandatory)*

### Primary User Flow

1. A user views any screen; all text is rendered in **Inter**.
2. Headings and body text follow a **consistent scale** — the same heading level looks the same size/weight on every screen.
3. Text uses the **defined colours** — primary text, secondary/muted text, links, and status colours (success/warning/danger) are consistent.
4. All icons (sidebar, header, lists, status/actions) are **Lucide outline** icons, visually consistent in stroke and size — no mixed icon styles.

### Acceptance Scenarios

| # | Given | When | Then |
|---|-------|------|------|
| 1 | Any screen is open | The user reads the text | The font is Inter throughout (no other typeface appears) |
| 2 | The same heading level appears on two screens | The user compares them | They share the same size and weight |
| 3 | The user views text colours | They inspect primary/secondary/link/status text | Each matches the defined palette |
| 4 | Any screen with icons is open | The user views the icons | All are Lucide outline icons with a consistent stroke and size for their context |
| 5 | The user scans the app for icons | They look across sidebar/header/tables/actions | No icons from the previous (filled) icon set remain |
| 6 | A design token (font, a type level, a colour) is changed centrally | The app re-renders | The change is reflected everywhere that token is used |

### Edge Cases

- **Icon with no exact Lucide equivalent** → a sensible Lucide alternative is chosen; no blank/placeholder icon is shown.
- **Font not yet loaded** → text still renders legibly with a fallback sans-serif until Inter loads.
- **Long text at a given type level** → wrapping/truncation behaves as before; the type scale does not break layout.
- **Icon-only controls** (e.g. row edit/delete, toggle) → remain recognizable with the Lucide equivalents at the correct size.

---

## Functional Requirements *(mandatory)*

Each requirement must be testable and unambiguous.

### Typography

- **FR-1**: The application MUST use **Inter** as its single typeface across all screens. *(Already applied.)*
- **FR-2**: A consistent **type scale** MUST be applied for headings and body text, matching the reference sheet — H1 (~32px, bold), H2 (~24px), H3 (~20px), H4 (~18px), Body Large (~16px), Body (~14px), Caption (~12px), with the sheet's weights — so a given level looks identical across screens.
- **FR-3**: Text colours MUST follow the defined palette — primary text, secondary text, muted text, link/primary, success, warning, and danger.

### Icons

- **FR-4**: The application MUST use the **Lucide outline** icon set as its single icon system, replacing the previously used icon set.
- **FR-5**: Every icon across the app (sidebar navigation, header/topbar, list/dashboard, and status/action icons) MUST be a Lucide outline icon, visually consistent in stroke style and sizing for its context.
- **FR-6**: Icons MUST render at consistent, context-appropriate sizes (e.g. navigation, in-button, inline/table) rather than ad-hoc sizes.
- **FR-7**: Every icon currently in the app MUST be mapped to an equivalent Lucide icon; no icon may be left missing, broken, or as a placeholder.
- **FR-8**: After migration, no icons from the previous (filled) icon set may remain in the app.

### Consistency

- **FR-9**: The typeface, type scale, text colours, and icon usage MUST be defined centrally (single source of truth) and reused, so changing a token propagates application-wide.

---

## Success Criteria *(mandatory)*

Measurable, technology-agnostic outcomes.

- **SC-1**: Inter renders on every screen; no other typeface appears.
- **SC-2**: Each heading/body level is visually identical across screens (same size + weight).
- **SC-3**: Primary/secondary/muted/link/status text colours match the defined palette.
- **SC-4**: 100% of icons are Lucide outline; none of the previous icon set remains anywhere.
- **SC-5**: Icons are consistent in stroke and sized appropriately per context (nav/button/inline).
- **SC-6**: Changing a central design token (font, a type level, or a colour) updates everywhere it is used.

---

## Scope

**In scope**:
- Inter as the single typeface (done) and a centralized, consistent heading/body type scale.
- The defined text-colour palette applied consistently.
- Replacing the entire icon set with Lucide outline icons across every screen and shared component, including mapping each existing icon to its Lucide equivalent.
- Defining these as central, reusable design tokens/styles.

**Out of scope**:
- Backend work, data, or schema changes (none).
- Restructuring layouts, navigation, or page content (covered by earlier specs) — only typography and icons change.
- Introducing new icons/features beyond mapping the icons already in use.
- A full dark-theme palette (only the text colours from the sheet are in scope here).

---

## Assumptions

- The reference sheet `docs/rules/angular/font/font.png` is the visual contract; exact px values and weights for each type level, and exact hex values for each colour, are read from it at `/implement` (the values in FR-2 are the read-back and may be fine-tuned to the sheet).
- **Inter is already applied** (font family + Material typography + base token); this feature completes the system (type scale, colours audit, icon migration).
- The icon migration replaces the current filled icon set entirely with Lucide outline; a Material→Lucide name mapping is produced during `/implement` (e.g. dashboard→layout-dashboard, edit→pencil, delete→trash-2, add→plus, notifications→bell, compare→git-compare, …).
- The text-colour palette aligns with the Tailwind scale already used in the app (e.g. gray-900/500/400, blue-600, green-600, red-500, amber-500), matching the sheet.

---

## Dependencies

- **`docs/rules/angular/font/font.png`** — the design-system reference (typography + icons).
- **`specs/001-frontend-foundation`** and the shell/layout specs (`004`–`007`) — the screens and components whose text and icons are restyled.
- All existing feature screens and shared components (every place that renders text or an icon).
