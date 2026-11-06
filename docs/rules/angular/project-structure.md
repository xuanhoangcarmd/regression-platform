# Purpose

Define a feature-based architecture for the latest standalone Angular app so the codebase is
scalable, maintainable, reusable, AI-friendly, and has clear separation of concerns. All
generated Angular code must follow this structure.

# Rules

## Root structure

```text
src/app/
├── core/        # app-wide singletons, loaded once
├── features/    # business features, self-contained
└── shared/      # reusable UI and utilities
```

- Each folder has a distinct responsibility and must not overlap.
- Use standalone components, directives, and pipes only — no `NgModule`.
- Lazy-load every feature via route-level `loadChildren`/`loadComponent`.

## Core layer (`core/`)

Application-wide functionality, loaded once and shared everywhere.

```text
core/
├── apis/            # API client services (HTTP only)
├── interceptors/    # auth, loading, error, correlation
├── services/        # app-wide services (auth, storage, settings, signalr)
├── models/          # global models (paged-result, problem-details, api-response)
├── constants/       # routes, api, storage, application constants
├── guards/
├── resolvers/
├── enums/
├── tokens/
└── configurations/
```

- **apis/**: one service per API domain (e.g. `project-api.service.ts`); HTTP communication only — no UI or business logic.
- **interceptors/**: handle authentication headers, error handling, loading state, request tracing.
- **services/**: shared across features; no feature-specific business logic.
- **models/**: global models used by multiple features.
- **constants/**: avoid magic strings, hardcoded route names, and duplicated keys.

## Features layer (`features/`)

Business functionality; each feature is self-contained.

```text
features/
├── dashboard/
├── projects/
├── api-definitions/
├── request-pool/
├── regression-runs/
├── compare/
├── reports/
└── settings/
```

Each feature follows:

```text
features/compare/
├── apis/         # API services used only by this feature
├── models/       # feature-specific DTOs
├── pages/        # route-level screen components
├── components/   # reusable components within the feature
├── services/     # feature-specific business logic / workflows
├── store/        # feature state (signals / signal store)
├── enums/
├── constants/
└── helpers/
```

- **apis/**: only used by this feature; move to `core/apis` if reused by multiple features.
- **models/**: feature DTOs; do not place in `core` unless reused.
- **pages/**: route-level screens responsible for page composition.
- **components/**: reusable inside the feature; move to `shared/components` if used by multiple features.
- **services/**: encapsulate feature workflows and keep components lightweight.
- **store/**: centralize feature state; use Angular Signals.

## Shared layer (`shared/`)

Reusable UI and utilities, generic and free of business logic.

```text
shared/
├── components/    # modal, loading, toastr, button, input, table, confirm-dialog, ...
├── layout/        # navbar, sidebar, footer, page-header, breadcrumbs
├── models/        # view models used across features
├── pipes/
├── services/      # notification.service, dialog.service, ...
├── helpers/       # date.helper, json.helper, ...
├── utils/         # string.util, number.util, ...
├── directives/
└── validators/
```

- **components/**: generic, reusable across features, no business logic.
- **models/**: only place here if used across multiple features.

## Barrel exports

- Every part/folder (core, shared, each feature, and their subfolders) exposes a barrel `index.ts` re-exporting its public members, grouped by kind with a comment header. Import from the barrel, not deep paths.

```ts
// constants
export * from './constants/auth.const';
export * from './constants/filter-page-keys.const';

// models
export * from './models/auth.model';
export * from './models/api.model';
export * from './models/app-config.model';

// interceptors
export * from './interceptors/auth.interceptor';

// guards
export * from './guards/auth.guard';
export * from './guards/public.guard';
```

## Component & state rules

- Components focus on UI, stay lightweight, and delegate business logic to services or the store.
- Components must NOT call `HttpClient` directly, contain complex business logic, or manage large state.
- `HttpClient` is used only inside API services — never injected into components, pages, pipes, or helpers.
- Data flow: `Page → Feature Service → API Service → Backend API`.
- State management: prefer Angular Signals / Signal Store; avoid component state duplication and shared mutable state.

## Reusability rule

- A component reused by two or more features → move to `shared/components`.
- A service reused by two or more features → move to `core/services`.

# AI Generation Requirements

When generating Angular code, the AI **must**:

- Follow the feature-based architecture and place code in the correct layer/folder.
- Use standalone components and lazy-loaded feature routes.
- Put route screens in `pages/`, reusable UI in `shared/components`, global services in `core/services`, and HTTP calls in `apis/`.
- Keep components lightweight and use Signals for state.
- Add/update the folder's `index.ts` barrel and import via the barrel.

The AI **must never**:

- Generate `NgModule`s or eagerly import all features.
- Place business logic in components.
- Inject `HttpClient` into pages, components, pipes, or helpers.
- Create a public member without exporting it from the folder's `index.ts`, or deep-import when a barrel exists.