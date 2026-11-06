# Database Architecture

Core entity relationships (actual table names — see `docs/database/`):

```text
Api
  │
  ▼
ApiRequest
  │
  ▼
Run ──────────► RunResponse
  │
  ▼
CompareResult ──► CompareDetail
```

- **Api** — API endpoint definitions.
- **ApiRequest** — reusable, versioned test requests (immutability via `Version` + `IsActive`).
- **Run** — one execution of a set of requests.
- **RunResponse** — one stored response per `(RunId, RequestId)` execution (immutable history).
- **CompareResult / CompareDetail** — summary + per-request diff for Run A vs Run B.
- **CompareJob** — async compare job (status/progress). **CompareIgnoreRule** — ignore paths.

> **⚠ Planned:** a `Project` entity (multi-project, project-scoped data) is a target but is
> **not** in the current schema. See `ImplementationStatus.md`.

Conventions: singular table names; `BIGINT IDENTITY` keys (except `Api` and `CompareIgnoreRule`
which use `INT`); audit via `CreatedDate` (UTC) on all tables (`UpdatedDate` on `Api`, `IsActive`
on `Api`/`ApiRequest`); index on `RunResponse (RunId, RequestId)`. See `docs/rules/database/`.

---

# Why ResponseHash

- Enables fast equality checks — compare hashes first and skip deep compare when they match.
- The hash is generated from **Canonical JSON** of already ignored + masked data, so it is stable across sprints regardless of formatting or property order.
- Supports duplicate and change detection cheaply, at scale (thousands of requests, millions of records).

# Why store encrypted responses (planned)

> **⚠ Planned / not yet implemented.** Today `RunResponse.ResponseJson` is plain text. The
> rationale below is the target design. See `ImplementationStatus.md`.

- Responses may contain sensitive payloads and PII; encryption (AES-256) protects data at rest for security and compliance.
- Encryption happens **after** hashing, so it never affects comparison — the hash is computed from canonical content, not ciphertext.
- Encryption keys are managed externally and never stored in source control.

See `Decisions.md` (ADR-002, ADR-003) and `docs/business/ResponseStorage.md`.
