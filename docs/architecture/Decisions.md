# Architecture Decision Records (ADR)

Records the key architecture decisions and their rationale. Add a new ADR for each
significant decision; do not edit accepted ADRs — supersede them with a new one.

---

# ADR-001 — Use Dapper instead of EF Core

**Reason:**

- Better performance
- Full SQL control
- Handles large response datasets efficiently

---

# ADR-002 — Compare by RequestId

**Reason:**

- Stable matching between runs
- Faster lookup

---

# ADR-003 — Store encrypted responses

**Reason:**

- Security
- Compliance

---

# ADR-004 — Apply Ignore Rules and Masking at capture time

**Reason:**

- Ignored fields directly affect comparison, so they are removed before hashing.
- Masking guarantees no PII/tokens are persisted.
- The hash is generated from already normalized + masked data, giving stable comparison across sprints.
- Encryption is applied last, for storage security only, and never affects the hash or comparison.

**Trade-off:** because ignore rules are baked in at capture, changing an ignore rule does not
retroactively affect already-stored runs.
