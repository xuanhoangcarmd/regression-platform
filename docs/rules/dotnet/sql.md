# Purpose

Keep SQL isolated from handlers and controllers so it is maintainable, reviewable, and reusable.

# Rules

- Never write SQL inside CommandHandler, QueryHandler, or Controller.
- Store SQL in dedicated `Sql` classes (e.g. `ApiDefinitionSql.cs`) under `Infrastructure/Persistence/Sql/`, or as external `.sql` files.
- SQL classes contain only SQL statements, query templates, and constants.
- Repositories execute SQL; handlers only call repositories.
- Flow: Controller → Handler → Repository → SQL class → SQL Server.

# AI Generation Requirements

When generating code, the AI **must**:

- Place SQL execution in repositories and SQL text in dedicated `Sql` classes or `.sql` files.
- Always use parameterized SQL and prefer constants/files for reusable queries.

The AI **must never**:

- Generate SQL inside a CommandHandler, QueryHandler, or Controller.
