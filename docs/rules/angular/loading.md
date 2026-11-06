# Purpose

Standardize loading effects so every async operation gives clear, consistent visual
feedback without blocking the UI or leaking state into components.

# Rules

- Track loading as state in the service that owns the async call: expose a read-only `loading` signal (e.g. `private _loading = signal(false)` → `loading = this._loading.asReadonly()`).
- Components read the `loading` signal and render feedback declaratively with `@if`.
- Use Angular Material indicators: `mat-progress-spinner`/`mat-spinner` for blocking waits, `mat-progress-bar` for inline/determinate progress.
- Prefer **skeleton placeholders** for content areas (lists, tables, cards) over full-page spinners to reduce layout shift.
- Disable submit/action buttons while their operation is loading; show an inline spinner inside the button.
- Use `@defer` with `@loading`/`@placeholder` blocks for heavy or below-the-fold components.
- Scope loading state to the smallest unit (per request/section), not a single global flag, so independent areas load independently.
- Always reset loading in a `finally`/completion path so it clears on both success and error.
- Style spacing/centering of loaders with Tailwind utilities; do not hardcode dimensions.

# AI Generation Requirements

When generating async UI, the AI **must**:

- Expose a read-only `loading` signal from the service and bind it in the template via `@if`.
- Use Material spinners/progress bars or skeletons, and disable in-flight actions.
- Reset loading state on both success and error.

The AI **must never**:

- Keep loading flags as mutable component fields or use a single global loading flag.
- Leave a request without loading feedback or forget to clear the loading state on error.
