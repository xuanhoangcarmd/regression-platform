# Purpose

Define indexing standards and document the **actual** indexes in `RegressionDB`.

---

# General Rules

- Every primary key → clustered index (on `Id`).
- Every foreign key → nonclustered index.
- Enforce business uniqueness with unique indexes.

---

# Actual Indexes

| Table | Index | Columns | Type |
|-------|-------|---------|------|
| Api | `IX_Api_Name` | `Name` | Unique nonclustered |
| ApiRequest | `IX_ApiRequest_ApiId` | `ApiId` | Nonclustered |
| RunResponse | `IX_RunResponse_Request` | `RequestId` | Nonclustered |
| RunResponse | `UX_Run_Request` | `RunId, RequestId` | Unique nonclustered |
| CompareResult | `UX_Compare_Run` | `RunAId, RunBId` | Unique nonclustered |
| CompareDetail | `IX_CompareDetail_Request` | `RequestId` | Nonclustered |
| CompareDetail | `IX_CompareDetail_Result` | `CompareResultId` | Nonclustered |

- `RunResponse (RunId, RequestId)` is the primary lookup/uniqueness key for run results.
- `CompareResult (RunAId, RunBId)` uniquely identifies a comparison between two runs.
