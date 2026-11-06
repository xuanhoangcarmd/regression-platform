# Purpose

Define an indexing strategy that keeps regression queries fast while avoiding write
overhead and redundant indexes on SQL Server.

# Rules

- Every table has a clustered primary key on `Id` (`BIGINT IDENTITY`).
- Add non-clustered indexes on all foreign key columns (e.g. `ProjectId`, `RunId`).
- `RunResponse` must have an index on `(RunId, RequestId)` — this is the primary lookup key.
- Index columns used in frequent `WHERE`, `JOIN`, and `ORDER BY` clauses.
- Use covering indexes (`INCLUDE`) for hot read paths to avoid key lookups.
- Use a filtered index for soft-delete reads where appropriate (e.g. `WHERE IsActive = 1`).
- Enforce uniqueness with a unique index/constraint (e.g. unique `(RunId, RequestId)`).
- Avoid: duplicate/overlapping indexes, indexing low-selectivity columns alone, and over-indexing write-heavy tables.
- Name indexes `IX_<Table>_<Columns>` (see naming rules).

# AI Generation Requirements

When generating schema or migrations, the AI **must**:

- Create a clustered PK on `Id` and non-clustered indexes on foreign keys.
- Add the `(RunId, RequestId)` index on `RunResponse`.
- Justify each index by a real query pattern and use `INCLUDE` for covering needs.

The AI **must never**:

- Create duplicate/overlapping indexes or index every column "just in case".
- Omit the `(RunId, RequestId)` index when generating `RunResponse`.
