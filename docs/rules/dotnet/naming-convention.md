# Purpose

Keep naming consistent across the solution so code is predictable and easy to navigate.

# Rules

- Projects: `RegressionPlatform.Api`, `.Application`, `.Domain`, `.Infrastructure`, etc.
- Classes: PascalCase (e.g. `ProjectRepository`, `CreateProjectCommand`).
- Interfaces: prefix with `I` (e.g. `IProjectRepository`, `IUnitOfWork`).
- Private fields: prefix with `_` (e.g. `_logger`, `_projectRepository`).
- Local variables: camelCase. Constants: PascalCase.
- SQL parameters: PascalCase with `@` (e.g. `@Id`, `@ProjectId`, `@RunId`).
- DTOs: suffix with `Request` or `Response` (e.g. `CreateProjectRequest`, `CreateProjectResponse`).

# AI Generation Requirements

When generating code, the AI **must**:

- Apply these casing and prefix/suffix conventions consistently.

The AI **must never**:

- Mix casing styles or omit `I`/`_`/`Request`/`Response` conventions.
