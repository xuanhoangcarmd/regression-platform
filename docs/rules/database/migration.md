# Purpose

Manage schema changes in a versioned, repeatable, and forward-only way so environments
stay consistent and deployments are safe.

# Rules

- All schema changes are made through versioned migration scripts checked into source control under `database/`.
- Never modify the schema manually in any shared environment.
- Scripts are named `YYYYMMDD_Description.sql` (e.g. `20260820_Create_RunResponse.sql`, `20260821_Add_ResponseHash_Index.sql`); date prefix keeps them ordered. See `docs/database/MigrationStrategy.md`.
- Migrations are forward-only and idempotent where possible (guard with `IF NOT EXISTS` / `IF OBJECT_ID(...) IS NULL`).
- Each migration is small, single-purpose, and reviewable; separate schema changes from large data backfills.
- Prefer additive, backward-compatible changes (add column/table) to support zero-downtime deploys; phase out columns in a later migration.
- Never edit a migration that has already been applied to a shared environment — create a new one.
- Keep `docs/database/` design docs in sync when the schema changes.

# AI Generation Requirements

When generating schema changes, the AI **must**:

- Produce a new, sortable, idempotent migration script under `database/`.
- Keep each migration focused and backward-compatible when possible.

The AI **must never**:

- Modify an already-applied migration or emit ad-hoc manual `ALTER` statements outside a migration.
- Generate destructive changes (drop column/table) without an explicit request.
