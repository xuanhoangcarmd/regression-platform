# Purpose

Standardize styling with Tailwind CSS as the primary approach, backed by SASS and Angular
Material, so the UI is consistent, themeable, and scoped per component.

# Rules

- **Use Tailwind CSS first** — build layout, spacing, sizing, typography, and most styling with Tailwind utility classes in the template.
- Drop to **SCSS** only for component-specific or complex styles that Tailwind can't express cleanly; keep those in the component's `.scss` file.
- Configure Tailwind centrally (`tailwind.config`) and drive its theme from the shared design tokens; do not duplicate token values.
- Rely on Angular's default component style encapsulation; do not use global styles for component-specific rules.
- Keep shared SCSS tokens (colors, spacing, typography, breakpoints) in a central `styles/` folder and `@use` them — never `@import` (deprecated).
- Define the Angular Material theme with the current `mat.theme()` / M3 theming APIs in one place; do not hardcode Material colors.
- Use design tokens / Tailwind theme values / CSS custom properties instead of magic numbers and raw hex values.
- Use Tailwind responsive prefixes (`sm:`, `md:`, `lg:`) for responsive design; SCSS mixins only when needed.
- Avoid `::ng-deep` and `!important`; if unavoidable, document why.

# AI Generation Requirements

When generating styles, the AI **must**:

- Style with Tailwind utility classes by default, falling back to component-scoped SCSS (`@use`, shared tokens) only when necessary.
- Theme Material and Tailwind centrally from shared tokens.

The AI **must never**:

- Write custom SCSS for what a Tailwind utility already provides.
- Use `@import`, `::ng-deep`, or `!important` casually.
- Hardcode colors/spacing or put component styles in global stylesheets.
