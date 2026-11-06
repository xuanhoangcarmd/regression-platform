# Database (DATA) — Index

How data is organized — schema, tables, relationships, indexes, and storage. **Read this index
first**, pick the document that matches your task, then read only that file — do not read the
whole folder.

> These are **design** docs (the concrete data model). The **rules** for writing schema/SQL
> live under `docs/rules/database/`.

## Overview & model

| Document | Read when… |
|----------|------------|
| `DatabaseDesign.md` | you need the data-model overview (principles, entities, flow) — start here |
| `ERD.md` | understanding entity relationships and cardinality |
| `Tables.md` | looking up what a specific table stores |
| `Relationships.md` | working with foreign key relationships |

## Conventions

| Document | Read when… |
|----------|------------|
| `NamingConvention.md` | naming tables, keys, or columns |
| `AuditColumns.md` | adding audit columns or soft delete |
| `IndexStrategy.md` | adding or tuning indexes |

## Storage

| Document | Read when… |
|----------|------------|
| `ResponseStorage.md` | working on the `RunResponse` table (hash/encrypt) |
| `CompareStorage.md` | working on `CompareResult` / `CompareDetail` |

## Operations & scale

| Document | Read when… |
|----------|------------|
| `MigrationStrategy.md` | writing schema migration scripts |
| `DataVolumeEstimation.md` | reasoning about data scale/volume |
| `PartitionStrategy.md` | partitioning large tables (`RunResponse`) |

Other perspectives: WHY = `docs/architecture/`, WHAT = `docs/business/`, HOW = `docs/rules/`.
