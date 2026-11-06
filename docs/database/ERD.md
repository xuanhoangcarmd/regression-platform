# Purpose

Document entity relationships. Reflects the **actual** `RegressionDB` schema.

---

# Relationships

| Parent | Cardinality | Child | Via |
|--------|-------------|-------|-----|
| Api | 1:N | ApiRequest | `ApiRequest.ApiId` |
| Run | 1:N | RunResponse | `RunResponse.RunId` |
| ApiRequest | 1:N | RunResponse | `RunResponse.RequestId` |
| Run | 1:N | CompareResult (as Run A) | `CompareResult.RunAId` |
| Run | 1:N | CompareResult (as Run B) | `CompareResult.RunBId` |
| CompareResult | 1:N | CompareDetail | `CompareDetail.CompareResultId` |
| ApiRequest | 1:N | CompareDetail | `CompareDetail.RequestId` |

`CompareJob` references `RunAId`, `RunBId`, and (when finished) `CompareResultId`, but the
schema declares no foreign keys on it. `CompareIgnoreRule` is standalone (no FK).

---

# Diagram

```text
Api ──1:N──> ApiRequest
              │   │
   ┌──────────┘   └──────────┐
   ▼ (RequestId)             ▼ (RequestId)
RunResponse              CompareDetail
   ▲                         ▲
   │ (RunId)                 │ (CompareResultId)
  Run ──1:N──> RunResponse  CompareResult
   │
   └──(RunAId / RunBId)──> CompareResult ──1:N──> CompareDetail

CompareJob  (RunAId, RunBId, CompareResultId — no FK)
CompareIgnoreRule  (standalone)
```
