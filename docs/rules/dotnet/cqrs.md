# Purpose

Enforce CQRS so state changes (Commands) and reads (Queries) stay separated, keeping
handlers small, focused, and free of data-access details.

# Rules

- Commands modify state; Queries read state. Never mix them.
- Organize by feature (e.g. `Features/ApiDefinition/Commands/Create`, `Queries/GetById`).
- A Query folder contains: `*Query.cs`, `*Handler.cs`, `*Response.cs`, and optional `*Validator.cs`.
- A Command folder contains: `*Command.cs`, `*Handler.cs`, `*Validator.cs`, and optional `*Response.cs`.
- Queries are read-only with no side effects; Commands create/update/delete and should not return large datasets.
- Handlers orchestrate only: validate, call repository, return response.
- Handlers must not execute SQL, access `DbConnection`, build SQL strings, or log directly.
- Access data via repository interface → implementation → SQL class; never bypass the repository.
- Map persistence models to response DTOs; never expose internal models.
- Validation uses FluentValidation and runs before business logic.
- Every handler accepts a `CancellationToken` and passes it to repository methods.
- Use strongly typed responses; avoid `object`/`dynamic`.
- One use case per handler; keep handlers small (≈100 lines max) and extract reusable logic into services.

# AI Generation Requirements

When generating code, the AI **must**:

- Produce feature-organized, single-use-case handlers with strongly typed responses.
- Validate with FluentValidation, accept and pass `CancellationToken`, and map to DTOs.
- Access data only through repository interfaces.

The AI **must never**:

- Mix Commands and Queries.
- Put SQL, `DbConnection` access, or direct logging in handlers.
- Return `object`/`dynamic` or generate oversized handlers.
