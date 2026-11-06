# CLAUDE.md

# Regression Platform

## Overview

Regression Platform is an AI-Driven API Regression Testing system.

The platform allows teams to:

- Manage multiple projects
- Manage API definitions
- Manage test requests
- Execute regression runs
- Store request and response history
- Compare responses between runs
- Detect breaking changes
- Generate AI-powered summaries
- Produce reports and dashboards

The architecture is designed for scalability and multi-project support.

> **Note:** some documented capabilities are **planned, not yet in the current schema** —
> multi-project (no `Project` table), response encryption, AI-summary persistence, and full
> `CreatedBy`/`UpdatedBy` auditing. See `docs/architecture/ImplementationStatus.md` for what is
> implemented vs planned, plus the business-concept → actual-table name mapping.

Examples of intended projects (multi-project is planned):

- O'Reilly
- AutoZone
- CARiD
- Internal Services

---

# Technology Stack

## Backend

- .NET 9
- ASP.NET Core Web API
- C#
- Dapper (primary data access — **no EF Core**, see `docs/architecture/Decisions.md` ADR-001)
- MediatR (CQRS)
- FluentValidation
- Serilog (structured logging)
- BackgroundService / Worker (regression run & compare execution)
- SQL Server

## Frontend

- Angular
- TypeScript
- Angular Material
- Tailwind CSS

## AI

- OpenAI
- Azure OpenAI (optional)

---

# MCP Servers

## Configured (`.mcp.json`)

- **database** — SSE transport at `http://localhost:5011/sse`. Use it to inspect the database, list/describe tables, and run read queries against SQL Server instead of guessing the schema.

## Expected / planned

The following MCP servers may also be available depending on the environment:

- filesystem
- git
- github
- playwright
- context7

Claude should prefer using MCP tools when appropriate instead of generating assumptions.

---

# Documentation

For a full project introduction (problem, objectives, users, features, workflow), read:

- **`docs/business/Introduction.md`** — start here for business context.

## Documentation model — 4 perspectives

The `docs/` folder is organized into four complementary perspectives: **WHY / WHAT / HOW /
DATA**. Together they give a complete picture.

**To save resources, do not read whole folders.** For each relevant perspective, **read its
`README.md` index first** — each index is a task-oriented "Read when…" catalog. Pick the one
or two documents that match your task, then read only those.

| Perspective | Index to read first | Answers |
|-------------|---------------------|---------|
| **WHY** | `docs/architecture/README.md` | Why the system is designed this way (design intent, trade-offs, ADRs) |
| **WHAT** | `docs/business/README.md` | What the system does (domain concepts, workflows, business rules) |
| **HOW** | `docs/rules/` (catalog below) | How to write the code (coding standards) |
| **DATA** | `docs/database/README.md` | How data is organized (schema, tables, relationships, indexes, storage) |

Workflow for any task:

1. Identify which perspective(s) the task touches (often more than one).
2. Open that perspective's `README.md` index (for HOW, use the rule catalog below).
3. From the "Read when…" table, choose only the matching document(s).
4. Read those documents, then proceed.

Read only what matches the task; skip the rest.

---

# Coding Guidelines

Project-specific coding rules live under `docs/rules/`, split into focused files.
**Do not duplicate these rules inside this file.**

## How to use the rule catalog

- This catalog is the index. **Read a rule file only when the task touches that topic**, then follow it.
- If a task does not relate to a rule, **skip it** to avoid wasting resources — do not read every file.
- Read the rule **before** generating/modifying the related code.
- When a task spans several areas, read only the matching rules (e.g. a new API endpoint → `cqrs`, `api-design`, `result-pattern`, `swagger`).
- Each rule file follows the same format: **Purpose / Rules / AI Generation Requirements**.

## Backend — `docs/rules/dotnet/`

| Rule file | Read when… |
|-----------|------------|
| `clean-architecture.md` | designing/placing code across layers; unsure where logic belongs |
| `project-structure.md` | creating projects/folders or organizing a feature |
| `cqrs.md` | adding a command/query, handler, or feature |
| `result-pattern.md` | defining handler/operation return types or error handling |
| `api-design.md` | adding/changing a controller, endpoint, route, or status code |
| `swagger.md` | documenting endpoints |
| `pagination.md` | building list/search endpoints |
| `mapping.md` | mapping between entities, DTOs, and responses |
| `repository.md` | adding/changing data-access repositories |
| `dapper.md` | writing data access / queries with Dapper |
| `sql.md` | writing or placing SQL in the backend |
| `naming-convention.md` | naming classes, interfaces, fields, DTOs, parameters |
| `logging.md` | adding logging or diagnostics |
| `middleware.md` | adding cross-cutting concerns (exceptions, correlation id, timing) |
| `authentication.md` | working on auth / JWT / identity |
| `authorization.md` | adding access control / policies |
| `caching.md` | adding caching |
| `configuration.md` | reading settings/secrets or adding config |
| `health-check.md` | adding/changing health endpoints |

## Database — `docs/rules/database/`

| Rule file | Read when… |
|-----------|------------|
| `naming.md` | naming tables, columns, constraints, indexes |
| `schema-design.md` | creating/altering tables, choosing columns or data types |
| `constraints.md` | defining PK/FK/unique/check/default constraints |
| `indexing.md` | adding indexes or tuning query performance |
| `migration.md` | writing schema migration scripts |
| `concurrency.md` | handling concurrent writes / optimistic concurrency |
| `security.md` | storing sensitive data or guarding against injection |

## Frontend — `docs/rules/angular/`

| Rule file | Read when… |
|-----------|------------|
| `project-structure.md` | adding a feature, folder, or barrel `index.ts` |
| `naming-convention.md` | naming files, components, services, models, enums, stores |
| `components.md` | creating/modifying components |
| `shared-components.md` | building reusable/generic UI in `shared/components` |
| `services.md` | adding business/reusable logic or services |
| `state-management.md` | managing shared/feature state with signals |
| `routing.md` | adding routes, guards, or lazy-loaded features |
| `http.md` | calling the backend API / data-access layer |
| `forms.md` | building forms, validation, or submit handling |
| `notifications.md` | showing toastr/snackbar notifications (success, error, info) |
| `tables.md` | building data tables, sorting, or paginated lists |
| `table-design.md` | styling tables (container, header, rows, badges, action icons, pagination) |
| `loading.md` | adding loading effects / async feedback (spinners, skeletons) |
| `styling.md` | styling UI (Tailwind-first, SCSS, Material theming) |
| `design-tokens.md` | applying visual design tokens (typography scale, color palette, icons, density) |

---

# Git Workflow

The repo uses git so every feature can be traced and rolled back on its own. Follow this for every task that changes code.

## Branch per feature

- Before starting a feature or fix, check `git status`. If the tree has uncommitted changes, stop and tell the user.
- Create a branch from `main`: `feature/<short-name>`, `fix/<short-name>` or `chore/<short-name>` (kebab-case, e.g. `feature/compare-order-mode`). If a spec exists, reuse its short name.
- Never commit feature work directly on `main`.

## Commit per step

- Commit small, logical steps (e.g. DB migration → backend → frontend → docs), not one big commit at the end.
- Commit a migration `database/YYYYMMDD_*.sql` together with the code that needs it, so the commit shows which code requires which migration.
- Message format: a short imperative subject (≤ 72 chars), a blank line, then a body explaining what and why. Mention any migration the user must run.
- End every commit message with the `Co-Authored-By` attribution line.

## Before every commit

- Backend changed: `dotnet build` must pass with 0 errors.
- Frontend changed: `ng lint` and `ng build` must pass.
- If a check fails, fix it or tell the user. Never commit broken code silently, and never use `--no-verify`.
- Run `git status` and the staged diff, and confirm that no secret is staged.

## Secrets — never commit

- Never commit `appsettings.json` / `appsettings.*.json` (only `appsettings.Example.json`, with values masked).
- Never commit `tools/`: it holds live credentials, API keys, tokens and customer data.
- Never copy keys or passwords into code, docs, specs or commit messages. Jira tickets contain live keys in curl samples.
- Keep `.gitignore` covering these. Stage files by explicit path rather than `git add -A`.

## Finishing and rolling back

- When the feature is done and verified, tell the user the branch and commit list. Merge into `main` with `git merge --no-ff` only when the user asks.
- Do not push, force-push, rebase shared history, or delete branches unless the user asks.
- To roll back a feature, use `git revert <commit>` (or revert the merge commit), not manual edits. A DB change still needs a separate rollback script, which the user runs.

---

# Priority

When multiple documents conflict, follow this priority:

1. CLAUDE.md
2. `docs/business/*`
3. `docs/architecture/*`
4. `docs/rules/*`
5. Existing project implementation

Always preserve consistency with the current architecture.
