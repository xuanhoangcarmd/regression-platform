# Purpose

Enforce Clean Architecture so the codebase stays maintainable, testable, and
framework-independent, with clear separation of concerns and inward-pointing dependencies.

# Rules

- Dependency direction is inward only: Presentation → Application → Domain.
- Infrastructure depends on Application and Domain; it implements interfaces defined by Application.
- Application must never depend on Infrastructure implementations.
- Domain must never depend on Infrastructure or Presentation, and must be framework-independent.
- Layer responsibilities:
  - **Presentation (API)**: HTTP endpoints, request binding, response formatting, auth. No business logic.
  - **Application**: use cases, commands, queries, workflows, validation orchestration.
  - **Domain**: entities, value objects, domain rules, enums.
  - **Infrastructure**: DB access (Dapper), external services, cache, storage, email, third-party APIs.
- Business logic lives only in Domain and Application — never in Controllers, repositories, or SQL classes.
- Application accesses data only through interfaces; it must never execute SQL directly.
- Organize code by feature (e.g. `Features/ApiDefinition/Commands`, `Queries`); avoid large shared service classes.
- Business logic must be testable without ASP.NET Core or SQL Server.

# AI Generation Requirements

When generating code, the AI **must**:

- Respect layer boundaries and feature-based organization.
- Define data-access interfaces in Application and implement them in Infrastructure.
- Keep classes small and focused.

The AI **must never**:

- Bypass the Application layer.
- Inject Infrastructure into Domain.
- Put business logic in Controllers, repositories, or SQL classes.
