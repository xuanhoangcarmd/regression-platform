# Frontend Architecture

The Angular app is organized into three top-level layers:

```text
src/app/
├── core/
├── features/
└── shared/
```

- **core/** — application-wide singletons loaded once (API clients, interceptors, guards, app services, global models/constants).
- **features/** — self-contained business features (dashboard, projects, api-definitions, request-pool, regression-runs, compare, reports, settings).
- **shared/** — reusable, generic UI and utilities (components, pipes, directives, helpers).

---

# Why separate Core

- Holds cross-cutting singletons that must exist once for the whole app (auth, HTTP, interceptors).
- Prevents feature code from depending on another feature for shared infrastructure.
- Keeps global concerns out of features, so features stay isolated and replaceable.

# Why separate Shared

- Holds generic, business-agnostic UI reused across multiple features.
- Avoids duplication and enforces a consistent design system.
- Shared code must never depend on a feature; feature-specific UI stays inside the feature.

# When to create a new Feature

- A new business capability with its own routes/screens.
- Functionality owned by a distinct part of the domain (e.g. compare vs reports).
- When code is used by only one area — keep it in that feature until 2+ features need it, then promote (UI → `shared/`, services → `core/`).

See the frontend rules under `docs/rules/angular/` for the detailed structure.
