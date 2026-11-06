# Compare Engine

## Purpose

Define how two regression runs are compared.

The Compare Engine is the core business capability of the platform.

Its responsibility is to identify:

* Added fields
* Removed fields
* Modified values
* Breaking changes

---

# Compare Inputs

* Run A — baseline (`CompareResult.RunAId`)
* Run B — target (`CompareResult.RunBId`)
* Project scope — both runs belong to the same project (`CompareJob.ProjectId`); only
  **Completed** runs are eligible.
* API scope — optional, via `CompareJobApi` (no rows = all of the project's APIs).

Example: Sprint 35 (Run A) vs Sprint 36 (Run B).

---

# Matching Strategy

Responses are matched by `RequestId`.

Example:

```text
Run A  RequestId = 1001
        ↓
Run B  RequestId = 1001
```

---

# Compare Flow

Ignore Rules, Canonical JSON, and the Response Hash are produced at capture time (see
Response Storage), so the stored hash already reflects ignored + masked + canonical data.
Comparison therefore works on stored hashes and only decrypts when a deep compare is needed.

```text
Match Request
        │
        ▼
Hash Compare                 (compare stored Response Hashes)
        │
        ▼  (only when hashes differ)
Load + Decrypt Responses
        │
        ▼
Deep Compare
        │
        ▼
Generate Compare Details
        │
        ▼
Generate Summary
```

---

# Ignore Rules

Ignore Rules are applied at capture time (during response storage), before Canonical JSON
and Hash Generation — so the fast Hash Compare already reflects the rules that existed when
the runs ran.

They are **also re-applied at compare time** on the deep-compare path: when two stored hashes
differ, the engine removes the currently-enabled ignore paths (scoped to the request's API)
from both stored canonical bodies and re-compares. This lets a rule added *after* the runs
were captured take effect **without re-running the runs** — if only ignored fields differ
(and status codes match) the request is reclassified **Unchanged**, and the ignored paths are
excluded from the diff. (Existing compare results are not retroactively changed; re-run the
compare to recompute with the current rules.)

Examples:

* `$.timestamp`
* `$.traceId`
* `$.requestId`

---

# Hash Compare

* Compare hashes first.
* The status code is compared alongside the hash: a request counts as **Changed** when the
  `ResponseHash` differs **or** the HTTP `StatusCode` differs (the hash covers the canonical
  body only, so a status-only change would otherwise be missed).
* If hashes match **and** status codes match: Result = **Unchanged**, and Deep Compare is skipped.
* If either side failed to produce a comparable response (error / no captured response):
  Result = **Failed**.

---

# Deep Compare

Executed only when hashes differ. Deep Compare identifies:

* Field Added
* Field Removed
* Value Changed
* Array Difference
* Object Difference

---

# Compare Detail

Stored in `CompareDetail`, one row per request:

* `CompareResultId`, `RequestId`
* `Result` — the per-request outcome (see below)
* `DiffJson` — field-level differences encoded as JSON

Each field difference inside `DiffJson` carries: JSON path, change type, old value, new value.

Example `DiffJson` entry:

```json
{ "path": "$.vehicle.year", "changeType": "ValueChanged", "old": 2024, "new": 2025 }
```

---

# Compare Result Types

`CompareDetail.Result` (per request):

* Unchanged — hashes and status codes match
* Changed — present in both runs but response hash or status code differs
* Added — present only in Run B
* Removed — present only in Run A
* Failed — compare errored or the response could not be compared

---

# Breaking Change Detection

Potential breaking changes:

* Field Removed
* Data Type Changed
* Required Field Removed

Non-breaking changes:

* New Optional Field
* Metadata Changes

---

# Compare Summary

Stored in `CompareResult`:

* `TotalRequest`
* `UnchangedCount`
* `ChangedCount`
* `FailedCount`
* `AddedCount`
* `RemovedCount`
* `ModifiedCount` (reserved; not populated by the current result set)

---

# AI Summary Integration

* Compare Engine provides structured data.
* AI generates human-readable summaries.
* AI must never modify comparison results.

---

# Performance Requirements

Always run:

```text
Hash Compare
        ↓
Deep Compare
```

* Never Deep Compare all responses by default.
* Support comparing thousands of requests and millions of response records.

---

# AI Generation Requirements

* Always compare by RequestId.
* Apply Ignore Rules at capture time, and re-apply the current rules on the deep-compare path
  (so rules added after capture still take effect).
* Compare hashes (and status codes) before deep comparison.
* Store per-request outcome in `CompareDetail.Result` and field diffs in `CompareDetail.DiffJson`.
* Store run-level counts in `CompareResult`.
* Separate Compare Engine from Web API concerns.
