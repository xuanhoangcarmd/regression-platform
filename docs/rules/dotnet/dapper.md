# Purpose

Standardize data access with Dapper as the primary ORM, ensuring safe, efficient, and
strongly typed queries.

# Rules

- Dapper is the primary data access library; EF Core must not be used.
- Use `IDbConnection`; keep connections short-lived — open only when needed, dispose immediately after execution.
- Map results to strongly typed DTOs; avoid dynamic objects.
- Always use parameterized queries (`WHERE Id = @Id`); never concatenate SQL strings.
- Use transactions only when multiple writes must succeed together; avoid unnecessary transaction scopes.
- Prefer projection over `SELECT *`; return only required columns.

# AI Generation Requirements

When generating code, the AI **must**:

- Use parameterized queries with strongly typed DTOs.
- Open connections only when needed and dispose them promptly.
- Select only required columns.

The AI **must never**:

- Use EF Core.
- Concatenate user input into SQL strings.
- Use `SELECT *` or return dynamic objects.
