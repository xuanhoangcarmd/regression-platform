# Purpose

Define routing for the standalone Angular app so features are lazy-loaded, navigation is
type-safe, and the route tree mirrors the feature structure.

# Rules

- Use standalone routing: bootstrap with `provideRouter(routes)`; no `RouterModule`.
- Each feature owns a `*.routes.ts` exporting its routes; the root routes lazy-load them with `loadChildren`/`loadComponent`.
- Lazy-load every feature (`dashboard`, `projects`, `api-definitions`, `request-pool`, `regression-runs`, `compare`, `reports`, `settings`).
- Use functional guards/resolvers (`CanActivateFn`, `ResolveFn`) with `inject()`; no class-based guards.
- Bind route params/query/data to component inputs via `withComponentInputBinding()`.
- Provide a wildcard `**` route for not-found and a sensible default redirect.
- Keep route paths kebab-case and aligned with feature folder names.

# AI Generation Requirements

When generating routes, the AI **must**:

- Define feature routes in `*.routes.ts` and lazy-load them from the root.
- Use functional guards/resolvers and component input binding.

The AI **must never**:

- Use `RouterModule.forRoot/forChild` or class-based guards.
- Eagerly import feature components into the root routes.
