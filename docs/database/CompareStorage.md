# Purpose

Define compare storage, based on the **actual** schema (`CompareJob`, `CompareJobApi`,
`CompareResult`, `CompareDetail`).

---

# CompareJob

Tracks an async comparison of two runs.

Fields:

- `ProjectId` (FK → Project) — compares are scoped to a project; both runs belong to it
- `RunAId`, `RunBId`
- `Status`
- `Progress` (0–100)
- `CompareResultId` (set when finished)
- `StartedDate`, `FinishedDate`
- `CreatedDate`

---

# CompareJobApi

Which APIs a compare targets (no rows = ALL APIs of the project). Mirrors `RunApi`.

Fields:

- `CompareJobId` (FK → CompareJob)
- `ApiId` (FK → Api)
- Unique on `(CompareJobId, ApiId)`

---

# CompareResult

Compare summary for Run A vs Run B.

Fields:

- `RunAId`, `RunBId`
- `TotalRequest`
- `UnchangedCount`
- `ChangedCount`
- `FailedCount`
- `AddedCount`
- `RemovedCount`
- `ModifiedCount` (reserved; not populated by the current result set)
- `CreatedDate`

No uniqueness on `(RunAId, RunBId)`: re-comparing the same pair creates a NEW result.

---

# CompareDetail

Per-request compare outcome and diff.

Fields:

- `CompareResultId` (FK → CompareResult)
- `RequestId` (FK → ApiRequest)
- `Result` — `varchar(20)`
- `DiffJson` — `nvarchar(MAX)`, field-level differences as JSON
- `CreatedDate`

---

# Result values

`Result` holds the per-request outcome, e.g.:

- Unchanged
- Changed
- Added
- Removed
- Failed

Field-level change types (Added / Removed / Modified / TypeChanged) are encoded inside
`DiffJson`, not as separate columns.

---

# Expected responses (baseline editing)

An admin can edit what a run's responses *should* be (`/projects/{code}/runs/{id}/expected`).
Edits live in `RunResponseOverride` — the captured `RunResponse` row is never modified.

| Table / column | Purpose |
|---|---|
| `RunResponseOverride (RunId, RequestId)` UNIQUE | One edited response per request of a run. Keyed by (RunId, RequestId) — not `RunResponse.Id` — so "Re-run request" (delete + insert) keeps the edit. `ON DELETE CASCADE` from `Run`. |
| `RunResponseOverride.ResponseJson / ResponseHash` | Canonical body + SHA-256, computed exactly like a capture, so the compare hash fast path still works. |
| `RunResponseOverride.StatusCode` | NULL = keep the captured status code. |
| `CompareJob.BaselineMode` | `Original` (default — captured only, the original behaviour) or `Expected` (each run — A and B — uses its own edit when one exists). |
| `CompareDetail.IsEdited` | The request was compared using an edit of either run. Display queries (diff viewer, test-results export) read each side from its edit only when this is 1. |

Classification rules are identical in both modes — only each run's hash / status / body source changes.
Migration: `database/20261007_AddExpectedResponses.sql` (also adds covering INCLUDEs on
`UX_Run_Request` and `IX_CompareDetail_Result`).

---

# Order mode (ignore order vs strict order)

| Stage | Ignore order (`CompareJob.IgnoreOrder = 1`, default) | Strict order (`IgnoreOrder = 0`) |
|---|---|---|
| Capture (`RunResponse`) | Same for both: `ResponseJson` = body in its **original key order** (rows before 2026-10-19 hold the key-sorted form); `ResponseHash` = SHA-256 of the key-sorted canonical body; `ResponseOrderedHash` = SHA-256 of the original-order body. | ← |
| Fast path | `ResponseHash` equal + status equal → Unchanged | `ResponseOrderedHash` (fallback `ResponseHash`) equal + status equal → Unchanged |
| Deep path | Key-sorted bodies; keyed arrays matched by identity (itemname/name/id/code/key) | Original order; arrays by position (LCS); a different key order → `OrderChanged` entry |
| Diff viewer | Bodies key-sorted, B's keyed arrays aligned to A | Bodies exactly as returned, no alignment |
| Report exports | Stored body as returned (original order) | ← |

Edited (expected) responses follow the same rule (`RunResponseOverride.ResponseOrderedHash`).
Migration: `database/20261019_AddCompareOrderMode.sql`.

---

# Compare rule kinds (`CompareIgnoreRule.RuleType`)

| RuleType | Effect at compare time (deep path) |
|---|---|
| `Ignore` | Drop the field at `JsonPath`. |
| `ExtractRoot` | Lift the subtree at `JsonPath` to the root (envelope unwrap). |
| `TrimAfter` | Compare string value(s) at `JsonPath` only up to the first `Delimiter` (e.g. `?` drops a signed URL's `Expires`/`Signature`). `[*]` applies to every array element. |

Rules are applied to the stored bodies when comparing (never at capture), so adding one takes effect on the next compare without re-running. Migration for `TrimAfter`: `database/20261020_AddIgnoreRuleTrimAfter.sql`.
