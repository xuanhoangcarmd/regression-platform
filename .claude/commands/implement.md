---
description: "Implement a feature directly from its specification, following Regression Platform rules."
argument-hint: "Optional: path to a feature dir or spec.md (defaults to the latest /specify feature)"
---

## User Input

```text
$ARGUMENTS
```

## Locate the Spec

1. If `$ARGUMENTS` points to a feature directory or a `spec.md`, use it.
2. Otherwise read `.specify/feature.json` and use its `feature_directory`.
3. Otherwise pick the `specs/` directory with the highest `NNN` prefix.
4. Set `SPEC_FILE` to `<FEATURE_DIR>/spec.md`. If it does not exist, ERROR and tell the user to run `/specify` first.

Read `SPEC_FILE` in full before doing anything else. The spec is the contract — implement
exactly what it says (Functional Requirements + Acceptance Scenarios + Success Criteria). If a
requirement still contains `[NEEDS CLARIFICATION]`, STOP and ask the user to resolve it.

## Mandatory Rules — read only what the feature touches

`CLAUDE.md` priority order applies: CLAUDE.md → `docs/business/*` → `docs/architecture/*` →
`docs/rules/*` → existing code. **Read the index, pick the matching rule(s), then read only those**
— do not read whole rule folders.

First confirm reality before coding:
- `docs/architecture/ImplementationStatus.md` — what is implemented vs **(Planned)**. Do not assume
  a `Project` table, response encryption, an AI-summary table, or `CreatedBy`/`UpdatedBy` exist.
- `docs/database/Tables.md` — the real `RegressionDB` schema (source of truth for names/types).

Then read rules by area. Use the `database` MCP server to inspect live schema instead of guessing.

**Backend (.NET 9 / Dapper / CQRS)** — read from `docs/rules/dotnet/` as relevant:
- New endpoint → `cqrs.md`, `api-design.md`, `result-pattern.md`, `swagger.md`
- Data access → `repository.md`, `dapper.md`, `sql.md`
- List/search → `pagination.md`; mapping DTOs → `mapping.md`
- Placement/structure → `clean-architecture.md`, `project-structure.md`, `naming-convention.md`
- Cross-cutting → `logging.md`, `middleware.md`, `configuration.md`, `caching.md`, `authentication.md`, `authorization.md`, `health-check.md`

**Database changes** — read from `docs/rules/database/` as relevant:
`schema-design.md`, `naming.md`, `constraints.md`, `indexing.md`, `migration.md`,
`concurrency.md`, `security.md`. Migration files: `YYYYMMDD_Description.sql`.

**Frontend (latest Angular / standalone / signals / Tailwind + Material)** — read from
`docs/rules/angular/` as relevant: `project-structure.md`, `naming-convention.md`, `components.md`,
`shared-components.md`, `services.md`, `state-management.md`, `routing.md`, `http.md`, `forms.md`,
`notifications.md`, `tables.md`, `loading.md`, `styling.md`.
Never put business logic in components — move reusable logic into services. Every folder exports
via a barrel `index.ts`.

## Implement

1. **Plan briefly**: from the spec, list the layers/files to add or change (backend, DB, frontend),
   then read only the rule files matching that list (above).
2. **Build it**, honoring the priority order and existing code conventions. Match surrounding style.
   Follow Clean Architecture + CQRS + Result pattern on the backend; standalone + signals on the frontend.
3. **Stay within the schema**: if the spec needs a column/table that doesn't exist, write a
   migration (`docs/rules/database/migration.md`) — do not silently assume it exists.
4. **Verify**: build the affected project(s) and run any relevant tests. Report failures with output.
   - Backend: `dotnet build` (and `dotnet test` if tests exist) for the touched project.
   - Frontend: `ng build` / `ng lint` for `src/frontend` if it touches the UI.

## Report

- Which spec was implemented (`FEATURE_DIR`).
- Files created/changed, grouped by layer.
- Which Functional Requirements / Acceptance Scenarios are now satisfied, and anything deferred.
- Build/test results (state failures honestly).
