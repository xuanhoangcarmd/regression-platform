# Purpose

Ignore dynamic fields during comparison.

---

# Examples

- `$.timestamp`
- `$.traceId`
- `$.requestId`
- `$.duration`

---

# Rules

- Ignore Rules are configurable.
- Ignore Rules are applied at capture time (during response storage), before:
  - Canonical JSON
  - Hash Generation
- Because ignored fields are removed before hashing and storage, the stored hash and stored
  response already exclude them — comparison does not re-apply Ignore Rules.

---

# Scope

- Global
- Project
- API
