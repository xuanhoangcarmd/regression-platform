# Purpose

Provide an overview of the database architecture. This document reflects the **actual**
SQL Server schema.

---

# Database

`RegressionDB` (schema `dbo`)

---

# Design Principles

- Normalize business data.
- Avoid duplicate storage.
- Optimize for read-heavy workloads.
- Support large response volumes.
- Support historical auditing.

---

# Core Tables

| Table | Key | Purpose |
|-------|-----|---------|
| `Api` | `int` | API endpoint definitions |
| `ApiRequest` | `bigint` | Reusable, versioned test requests for an Api |
| `Run` | `bigint` | A regression run (execution) |
| `RunResponse` | `bigint` | One captured response per (Run, Request) |
| `CompareJob` | `bigint` | Async compare job (status/progress) for two runs |
| `CompareResult` | `bigint` | Compare summary for Run A vs Run B |
| `CompareDetail` | `bigint` | Per-request compare outcome + diff |
| `CompareIgnoreRule` | `int` | Configurable JSON paths to ignore during compare |

---

# Data Flow

```text
Api
  ↓
ApiRequest
  ↓
Run  ──────────────► RunResponse
  ↓                       ▲
CompareJob                │
  ↓                       │
CompareResult ────────────┘  (compares Run A vs Run B responses)
  ↓
CompareDetail
```

`CompareIgnoreRule` is applied during comparison (independent of the entity chain).
