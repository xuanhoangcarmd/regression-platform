# Purpose

Define how Angular components are designed and written so they stay thin, reusable,
maintainable, testable, and free of business logic, with clear separation of concerns.

# Rules

## Mechanics (latest Angular)

- Components are **standalone** (`standalone: true`); no `NgModule`.
- Use `ChangeDetectionStrategy.OnPush` on every component.
- Use signals for component state; use `input()`/`output()` functions instead of `@Input()`/`@Output()` decorators.
- Use `computed()` for derived state and `effect()` only for side effects.
- Inject dependencies with `inject()` rather than constructor parameters where practical.
- Use the new control-flow syntax in templates: `@if`, `@for` (with `track`), `@switch` — not `*ngIf`/`*ngFor`.
- Use `@defer` for heavy or below-the-fold content.
- Keep templates declarative; avoid complex logic in templates — expose `computed()` signals instead.
- Unsubscribe is unnecessary when using signals/`toSignal`/`async` pipe; never manually subscribe without cleanup.
- Use the `async` pipe or `toSignal()` to render observables; do not call `.subscribe()` in components to set fields.

## Component categories

The app uses three component categories:

- **Page components** — in `features/*/pages`. Route entry points; compose the page and trigger feature workflows (e.g. `CompareListPageComponent`, `CompareDetailPageComponent`, `RunHistoryPageComponent`).
- **Feature components** — in `features/*/components`. Reusable within one feature; UI composition and feature-specific interactions (e.g. `CompareSummaryCardComponent`, `CompareFilterComponent`, `CompareDiffViewerComponent`).
- **Shared components** — in `shared/components`. Generic UI reusable across features (e.g. `ButtonComponent`, `ModalComponent`, `DataTableComponent`, `DatePickerComponent`).

## Responsibilities

Components **should**: render UI, receive inputs, emit events, and delegate business logic.

Components **should NOT**: call `HttpClient`, build API requests, execute business logic, or contain SQL/persistence knowledge.

## Smart vs presentational

- Prefer **presentational (dumb)** components: inputs/outputs only, no API calls.
- **Container (smart)** components coordinate workflows and interact with services/store.
- Communicate via `input()`/`output()`; avoid deeply nested component communication.

## State scope

- **Local UI state** → the component (signals).
- **Feature state** → feature store / signal store.
- **Global state** → core service.
- All business/data logic lives in services, never in components.

# AI Generation Requirements

When generating components, the AI **must**:

- Produce standalone, `OnPush` components using signals, `input()`/`output()`, and `inject()`.
- Use `@if`/`@for`/`@switch` and delegate logic to services.
- Generate small, focused components (avoid components larger than ~300 lines).
- Place route screens in `features/*/pages`, feature UI in `features/*/components`, and generic UI in `shared/components`.
- Prefer presentational components, promoting reusable UI to `shared/components`.

The AI **must never**:

- Generate `NgModule`s, `*ngIf`/`*ngFor`, or decorator-based `@Input()`/`@Output()`.
- Put business logic in a component, call `HttpClient`/build API requests from a component, or manually subscribe without cleanup.
