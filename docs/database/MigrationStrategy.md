# Purpose

Define the database schema change process.

---

# Rules

- Schema changes must be versioned.
- Do not modify production tables manually.
- All schema changes must be scripted.

---

# Migration Naming

`YYYYMMDD_Description.sql`

Examples:

- `20260820_Create_RunResponse.sql`
- `20260821_Add_ResponseHash_Index.sql`

---

# Backward Compatibility

Avoid breaking schema changes. Prefer additive changes.

Prefer:

- Add Column
- Add Table
- Add Index

Avoid:

- Drop Column
- Rename Column
