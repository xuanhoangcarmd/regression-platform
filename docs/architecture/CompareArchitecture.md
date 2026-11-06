# Compare Architecture

The Compare Engine is the core capability of the platform. It compares two regression runs
and produces structured, field-level differences.

Conceptual end-to-end pipeline (Ignore Rules + Canonical JSON + Hash are produced at
**capture time** during response storage; at compare runtime the engine starts from stored
hashes):

```text
Run A        Run B
    │           │
    └─────┬─────┘
          ▼
   Match RequestId
          │
          ▼
   Ignore Rules         (applied at capture time)
          │
          ▼
   Canonical JSON       (applied at capture time)
          │
          ▼
   Hash Compare         (compare stored Response Hashes)
          │
          ▼ (only when hashes differ)
   Deep Compare
          │
          ▼
   Compare Result
```

---

# Why compare by RequestId

- `RequestId` provides stable, deterministic matching between runs even as ordering changes.
- Lookups are fast and unambiguous — one request in Run A maps to exactly one in Run B.
- Avoids fragile matching by index or content.

# Why hash first

- Hash equality is an O(1) check; identical hashes mean identical (ignored + masked + canonical) responses → result = Match, no deep compare needed.
- This keeps comparison cheap at scale; deep compare runs only for the responses that actually changed.

# Why deep compare second

- Executed only when hashes differ, to find the exact differences.
- Produces Compare Details (JsonPath, ChangeType, OldValue, NewValue) and classifies breaking vs non-breaking changes.
- Re-applies the currently-enabled Ignore Rules to both stored canonical bodies before diffing, so rules added after capture take effect (only-ignored-fields-differ → reclassified Unchanged); existing results are recomputed by re-running the compare.

See `Decisions.md` (ADR-002), `docs/business/CompareEngine.md`, and `docs/business/CompareRules.md`.
