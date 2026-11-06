# System Architecture

The highest-level document describing the entire Regression Platform. Start here, then drill
into the focused architecture docs (backend, frontend, database, compare, AI, security,
deployment) and the recorded decisions in `Decisions.md`.

---

# Technical Layers

```text
Angular
    │
    ▼
.NET API
    │
    ▼
Application
    │
    ▼
Repository
    │
    ▼
SQL Server
```

- **Angular** — UI and user interaction.
- **.NET API** — HTTP endpoints; receives requests, returns responses (no business logic).
- **Application** — use cases via CQRS (commands/queries, handlers, validation).
- **Repository** — data access via Dapper.
- **SQL Server** — persistent storage.

Dependencies point inward (Clean Architecture): Presentation → Application → Domain, with
Infrastructure implementing Application interfaces.

---

# Functional Flow

```text
Run
    │
    ▼
Response Storage
    │
    ▼
Compare Engine
    │
    ▼
AI Summary
```

- **Run** — execute a collection of requests against a target environment.
- **Response Storage** — normalize, mask, hash, encrypt, and persist each response.
- **Compare Engine** — compare two runs and produce structured differences.
- **AI Summary** — generate a human-readable explanation from the compare result.

---

# Related Documents

- `BackendArchitecture.md`
- `FrontendArchitecture.md`
- `DatabaseArchitecture.md`
- `CompareArchitecture.md`
- `AIArchitecture.md`
- `SecurityArchitecture.md`
- `DeploymentArchitecture.md`
- `Decisions.md`
