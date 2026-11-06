# Rules (HOW)

How to write the code — coding standards split into focused files. Read the matching rule
**before** generating or modifying the related code; skip unrelated rules to save resources.

Each rule file follows the same format: **Purpose / Rules / AI Generation Requirements**.

The full, task-oriented rule catalog ("read when…") lives in `CLAUDE.md` under
**Coding Guidelines**. The folders are:

| Folder | Scope |
|--------|-------|
| `docs/rules/dotnet/` | Backend: Clean Architecture, CQRS, API design, Dapper/SQL, logging, auth, caching, config, etc. |
| `docs/rules/database/` | Schema/SQL rules: naming, schema design, constraints, indexing, migration, concurrency, security |
| `docs/rules/angular/` | Frontend: project structure, components, services, state, routing, http, forms, tables, styling, etc. |

Related perspectives: architecture (WHY) = `docs/architecture/`, business (WHAT) = `docs/business/`, data (DATA) = `docs/database/`.
