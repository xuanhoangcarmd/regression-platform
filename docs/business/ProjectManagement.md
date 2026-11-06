> **⚠ Planned / not yet implemented.** Multi-project is a target design — the current schema
> has no `Project` table and `Api` has no `ProjectId`. See `docs/architecture/ImplementationStatus.md`.

# Purpose

A Project represents a business system under test.

Examples:

- O'Reilly
- AutoZone
- CARiD

---

# Rules

A Project:

- owns APIs
- owns Requests
- owns Runs
- owns Compare Results

Every API belongs to exactly one Project.

A Project may contain multiple environments.

Examples:

- DEV
- QA
- UAT
- PROD
