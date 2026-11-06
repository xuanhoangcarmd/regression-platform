# Feature Specification: [FEATURE NAME]

**Feature Directory**: `[specs/NNN-short-name]`
**Created**: [DATE]
**Status**: Draft

> Focus on **WHAT** and **WHY**, not HOW. Written for stakeholders, not implementers.
> Remove any section that does not apply (do not leave "N/A").

---

## Summary

[1–3 sentences: what this feature does and the value it delivers.]

---

## User Scenarios & Testing *(mandatory)*

### Primary User Flow

[Describe the main happy path as a numbered sequence of user/system steps.]

1. ...
2. ...

### Acceptance Scenarios

| # | Given | When | Then |
|---|-------|------|------|
| 1 | [state] | [action] | [observable outcome] |

### Edge Cases

- [Boundary / error / empty / large-volume case and expected behavior]

---

## Functional Requirements *(mandatory)*

Each requirement must be testable and unambiguous.

- **FR-1**: The system MUST ...
- **FR-2**: The system MUST ...
- **FR-3**: Users MUST be able to ...

---

## Key Entities *(include if data is involved)*

Map to the real `RegressionDB` schema where possible (see `docs/database/Tables.md`).
Flag anything not yet in the schema as **(Planned)** per `docs/architecture/ImplementationStatus.md`.

| Entity | Maps to table | Notes |
|--------|---------------|-------|
| [Name] | `[Table]` | [key fields / relationships] |

---

## Success Criteria *(mandatory)*

Measurable, technology-agnostic outcomes.

- **SC-1**: [e.g. "Users can compare two runs and see results in under 3 seconds for 10k requests"]
- **SC-2**: ...

---

## Scope

**In scope**: [bullets]
**Out of scope**: [bullets]

---

## Assumptions

- [Reasonable defaults applied where the description was silent]

---

## Dependencies

- [Other features, tables, services, or external systems this relies on]
