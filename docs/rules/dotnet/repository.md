# Purpose

Define the role of repositories as the only place for data access, keeping them free of
business logic.

# Rules

- Repositories handle data access only: execute SQL, map results, return DTOs/models, handle persistence.
- Repositories must not validate business rules, call external APIs, generate responses, or perform calculations.
- One explicit repository per aggregate or feature (e.g. `ApiDefinitionRepository`, `RegressionRunRepository`). Avoid a generic repository.
- SQL belongs inside the repository or a dedicated SQL provider — never in handlers.
- Repositories implement interfaces defined in the Application layer (e.g. `IApiDefinitionRepository`).
- Every repository method accepts and passes a `CancellationToken`.

# AI Generation Requirements

When generating code, the AI **must**:

- Create explicit, per-feature repositories implementing Application interfaces.
- Keep SQL execution and result mapping inside the repository.

The AI **must never**:

- Put business logic, validation, or external API calls in a repository.
- Generate a generic/base repository.
- Leave SQL in handlers or controllers.
