# Purpose

Define how response comparison works.

---

# Comparison Flow

Canonical JSON and the Response Hash are produced at capture time (see Response Storage),
so comparison starts from the stored hashes:

```text
Match Request
      ↓
Hash Compare
      ↓
Deep Compare        (only when hashes differ)
      ↓
Generate Diff
```

---

# Hash Compare

Responses are normalized before hashing.

Ignore:

- Property order
- Formatting

The HTTP status code is compared alongside the hash — a status-only change (same body,
different status) still counts as Changed, since the hash covers the canonical body only.

---

# Deep Compare

Executed only when hashes differ. The currently-enabled Ignore Rules are re-applied to both
stored canonical bodies first, so rules added after the runs were captured still take effect
(if only ignored fields differ, the request becomes Unchanged).

---

# Compare Types

- Field Added
- Field Removed
- Value Changed
- Array Difference
- Object Difference
