# Purpose

Define the visual design tokens (typography, color, icons, density) for the Regression
Platform UI so every screen looks consistent, professional, and data-dense. The system is the
**"Technical Precision System"** (from the Stitch `DESIGN.md`) — a blue-led, M3-style semantic
palette — and is the single source of truth for look-and-feel.

They feed the **shared design tokens** referenced in [`styling.md`](styling.md): drive the
Tailwind theme and Angular Material (M3) theme from these values — never hardcode hex codes or
sizes in components.

## Single source of truth (implementation)

All token *values* live in **one file** as CSS custom properties. To recolor the app, retune
typography, or adjust dark mode, edit only this file — Tailwind, the global styles, and Angular
Material all read from it via `var(--rp-*)`.

| Concern | File | Notes |
|---------|------|-------|
| **Token values + dark mode** | `src/styles/_tokens.scss` | `:root` (light) and `:root[data-theme='dark']` (dark overrides). **Edit colors here.** |
| Tailwind theme | `tailwind.config.js` | References `var(--rp-*)` only — no hardcoded hex. |
| Global base + Material light/dark themes | `src/styles.scss` | Material colors swap under `:root[data-theme='dark']`. |
| Dark-mode switch | `core/services/theme.service.ts` (`ThemeService`) | Toggles `data-theme` on `<html>`, persists to `localStorage`, honors system preference. |
| Icon names | `core/constants/app-icons.const.ts` (`AppIconsConstants`) | Semantic key → Material Symbols name; swap an icon in one place. |

**Rules:**
- Components consume tokens via **Tailwind utility classes** (e.g. `bg-surface text-on-surface`,
  `text-secondary`, `border-outline-variant`) or `var(--rp-*)` directly. Never hardcode hex/size,
  and do not reintroduce per-file SCSS copies of the palette.
- To toggle theme, inject `ThemeService` and call `toggle()` / `setMode('dark')`.
- Reference icons via `AppIconsConstants` (e.g. `AppIconsConstants.runs`), not raw strings.

## Dark mode

`ThemeService` sets `data-theme="dark"` on the root element. That single attribute:

1. Switches every CSS custom property to its dark value (the `[data-theme='dark']` block).
2. Re-themes Angular Material (the dark M3 color set is applied under the same selector).

Because Tailwind classes resolve to the same variables, **no component needs `dark:` variants for
token colors** — `bg-surface` is light in light mode and dark in dark mode automatically. Use
Tailwind's `dark:` prefix only for the rare case that isn't covered by a token.

> **Stack note:** The original Stitch spec suggested Ant Design, Redux Toolkit, Recharts, and
> Lucide React. Those are **React** libraries and do **not** apply here. Per CLAUDE.md the
> frontend is **Angular + Angular Material + Tailwind CSS**. Only the *visual* tokens below are
> normative; the framework mapping is given so the same look is achieved with the Angular stack.

# Rules

## 1. Typography

Type scale with three families: **Hanken Grotesk** (display/headings), **Inter** (body/UI/labels),
**JetBrains Mono** (code/endpoints/IDs). Each token sets both a `font-*` (family) and `text-*` (size).

| Token | Family | Size / line / weight | Usage |
|-------|--------|----------------------|-------|
| `display-lg` | Hanken Grotesk | 30 / 38 / 700 | Hero / large page titles |
| `headline-md` | Hanken Grotesk | 20 / 28 / 600 | Page titles |
| `title-sm` | Hanken Grotesk | 16 / 24 / 600 | Card / section / table titles |
| `body-md` | Inter | 14 / 20 / 400 | Default body & table data |
| `body-sm` | Inter | 12 / 18 / 400 | Dense rows, secondary text |
| `label-xs` | Inter | 11 / 14 / 600 (0.05em, uppercase) | Table headers, chips, labels |
| `code-sm` | JetBrains Mono | 12 / 16 / 400 | Endpoints, JSON, Rule IDs |

- Body default is `body-md` (14px). Use `font-<token> text-<token>` together (e.g. `font-headline-md text-headline-md`).

## 2. Color Palette

M3-style semantic tokens (light values; dark overrides in `_tokens.scss`). Pair each surface with
its `on-*` text color for contrast.

**Brand / interactive**

| Token | Light hex | Usage |
|-------|-----------|-------|
| `primary` | `#0037b0` | Primary actions, active nav, links |
| `on-primary` | `#ffffff` | Text/icon on `primary` |
| `primary-container` | `#1d4ed8` | Brighter primary fills (buttons, avatar) |
| `on-primary-container` | `#cad3ff` | Text on `primary-container` |
| `secondary` | `#505f76` | Secondary / muted text & icons |
| `error` | `#ba1a1a` | Failed / destructive |

**Surfaces & text**

| Token | Light hex | Usage |
|-------|-----------|-------|
| `background` / `surface` | `#faf8ff` | App / page background |
| `surface-container-low/​/high/highest` | `#f3f2fe … #e2e1ed` | Layered card/panel fills |
| `on-surface` | `#1a1b23` | Primary text |
| `on-surface-variant` | `#434655` | Secondary text |
| `outline` / `outline-variant` | `#747686` / `#c4c5d7` | Borders, dividers |

**Status** — use Tailwind's default `green`/`amber`/`red` scales for chips (success/warning/failed),
and the `error` token for destructive UI, matching the design.

**Sidebar rail** (always dark, in both light & dark themes)

| Token | Value | Usage |
|-------|-------|-------|
| `sidebar` | `#2e3039` | Rail background |
| `sidebar-on` | `#ffffff` | Rail text/icons |
| `sidebar-muted` | `white/70` | Inactive rail items |
| `sidebar-hover` | `white/10` | Hover fill |
| `sidebar-active` | `#0037b0` | Active item / expanded project |
| `sidebar-line` | `white/10` | Nested guide line + dividers |

## 3. Icons

- Use **thin, sharp line (outline) icons**.
- In the Angular stack, use **Angular Material icons** (`<mat-icon>`, **Material Symbols Outlined**
  set, configured as the default `fontSet`) — do **not** add `lucide-react` or Ant Design icons.
- Icon names are centralized in `AppIconsConstants` (`core/constants/app-icons.const.ts`).
  Reference them by key (e.g. `AppIconsConstants.runs`); the mapping for primary navigation:

| Concept | Material Symbol |
|---------|-----------------|
| Dashboard (top) | `home` |
| Projects | `folder` |
| APIs | `code` |
| Request Pool | `inbox` |
| Runs | `history` |
| Compare | `compare` |
| Reports | `description` |
| Ignore Rules | `rule` |
| Configurations | `assignment` |
| Settings | `settings` |

## 4. Framework mapping (Angular)

| Stitch spec (React) | Use instead (this project) |
|---------------------|----------------------------|
| Ant Design components | Angular Material + Tailwind ([`shared-components.md`](shared-components.md)) |
| Redux Toolkit / React Query | Angular signals state ([`state-management.md`](state-management.md)) |
| Recharts / Ant Design Charts | Project-approved Angular chart lib |
| Lucide React / Antd Icons | Angular Material (outlined) icons |
| Styled Components | Tailwind-first, scoped SCSS ([`styling.md`](styling.md)) |

# AI Generation Requirements

When generating UI, the AI **must**:

- Consume tokens via Tailwind classes (`bg-surface`, `text-on-surface`, `text-secondary`,
  `border-outline-variant`, …) or `var(--rp-*)`. New/changed token values go **only** in
  `src/styles/_tokens.scss`.
- Use `body-md` (14px) as the default text size and pair `font-<token>` with `text-<token>`.
- Use `green`/`amber`/`red`/`error` consistently for run/compare outcomes (passed/pending/failed).
- Use `<mat-icon>` with names from `AppIconsConstants` (Material Symbols Outlined).
- Toggle theme through `ThemeService`; rely on tokens for dark mode rather than `dark:` variants.

The AI **must never**:

- Hardcode hex values or font sizes in components, or duplicate the palette in a per-file SCSS
  copy — the only place values live is `src/styles/_tokens.scss`.
- Introduce React-only libraries (Ant Design, Redux Toolkit, Recharts, Lucide React) into this
  Angular project.
- Drop table/body text below 14px or invent off-palette colors.
