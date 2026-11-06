# Purpose

Compare two Regression Runs.

---

# Inputs

- Run A — baseline (`RunAId`)
- Run B — target (`RunBId`)

---

# Matching Logic

Compare requests using `RequestId`.

Example:

```text
Run A RequestId 1001
        ↓
Run B RequestId 1001
```

---

# Output

Each compared request resolves to a `CompareDetail.Result`:

- Unchanged
- Changed
- Added
- Removed
- Failed

---

# Compare Result

Summary stored in `CompareResult`:

- `TotalRequest`
- `UnchangedCount`
- `ChangedCount`
- `FailedCount`
- `AddedCount`
- `RemovedCount`
- `ModifiedCount` (reserved)
