# Purpose

Document the audit columns used in the **actual** `RegressionDB` schema.

---

# Columns in use

| Column | Where | Notes |
|--------|-------|-------|
| `CreatedDate` | every table | `datetime2 DEFAULT getutcdate()` (UTC) |
| `UpdatedDate` | `Api` only | `datetime2 NULL` |
| `IsActive` | `Api`, `ApiRequest` | `bit DEFAULT 1` |

> The current schema does **not** include `CreatedBy` / `UpdatedBy`. Full "who changed it"
> auditing is a **planned** target (the rules treat it as the standard); those columns must be
> added to the schema when implemented. See `docs/architecture/ImplementationStatus.md`.

---

# Date Standard

All timestamps are stored as UTC via `getutcdate()`.

---

# Soft Delete

- `Api` and `ApiRequest` support soft delete via `IsActive`.
- Other tables (`Run`, `RunResponse`, `CompareResult`, `CompareDetail`, `CompareJob`) are
  append-only history and have no `IsActive` column.
