# Purpose

Standardize state management with a signal-first approach that keeps state predictable,
centralized, and out of components.

# Rules

## Signal primitives

- Use Angular **signals** as the default state primitive: `signal()`, `computed()`, `effect()`.
- Avoid introducing RxJS `Subject`s for local state; reach for a store library only when feature complexity grows.
- Convert server streams with `toSignal()`; convert signals to streams with `toObservable()` only when an RxJS operator is required.

## State scope

- **Local UI state** → the component via `signal()` (e.g. `loading`, `selectedItem`, `expandedRow`).
- **Derived state** → `computed()` (e.g. `filteredItems`, `totalCount`, `selectedRun`); never duplicate derived values.
- **Side effects** → `effect()` only (e.g. load on filter change, refresh list, sync URL params); never to mutate state in a loop.
- **Feature state** → the feature `store/` (e.g. `compare.store.ts`, `run.store.ts`).
- **Global state** → `core/services` or `core/store` (e.g. current user, theme, settings).

## Mutations

- Hold writable state as private `signal()` in the service/store; expose it read-only via `.asReadonly()` or `computed()`.
- Change state only through dedicated, intention-revealing methods (e.g. `loadRuns()`, `selectRun()`, `refresh()`).
- Never mutate a signal directly from multiple components.
- Keep state immutable — replace objects/arrays rather than mutating in place.
- Components read state from injected service/store signals; they do not own cross-component state.

# AI Generation Requirements

When managing state, the AI **must**:

- Prefer signals (and `computed()` for derived state) over RxJS Subjects.
- Store feature/global state centrally (feature `store/`, `core/services`/`core/store`) and expose it read-only.
- Mutate state only through dedicated methods, using immutable updates.
- Use a signal store as feature complexity increases.

The AI **must never**:

- Keep shared/business state inside components or duplicate state.
- Mutate state directly from components or mutate objects in place.
- Overuse `effect()` for state changes.
