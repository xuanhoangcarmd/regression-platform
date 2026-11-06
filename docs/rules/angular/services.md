# Purpose

Centralize business logic, state, and data access in services so components remain thin and
logic stays reusable and testable.

# Rules

- All business logic and reusable logic lives in services, never in components.
- Provide app-wide singletons with `@Injectable({ providedIn: 'root' })`; scope feature-only services to the feature's route providers.
- Inject dependencies with `inject()`.
- Expose state as signals (or `Observable`s converted via `toSignal()`); keep mutable state private and expose read-only signals.
- Services that talk to the backend use the typed HTTP data-access layer (see http rules) — components never call `HttpClient` directly.
- Keep services single-responsibility; split data access, state, and orchestration when they grow.
- Strongly type all inputs/outputs with interfaces/models; avoid `any`.

# AI Generation Requirements

When generating services, the AI **must**:

- Place business/data logic in `@Injectable` services using `inject()` and signals.
- Expose read-only state and strongly typed APIs.

The AI **must never**:

- Leave business logic in components or expose mutable internal state directly.
- Use `any` for service inputs/outputs.
