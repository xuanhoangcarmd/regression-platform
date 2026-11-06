# Purpose

Define how API responses are stored, based on the **actual** `RunResponse` table.

---

# Table

`RunResponse`

---

# Stored Data

- `RunId` (FK → Run)
- `RequestId` (FK → ApiRequest)
- `StatusCode`
- `DurationMs`
- `ResponseHash` — `char(64)`, SHA-256 of canonical JSON
- `ResponseJson` — `nvarchar(MAX)`, the stored response body
- `ResponseSize`
- `ErrorMessage`
- `CreatedDate`

---

# Rules

- Store the hash (`ResponseHash`) separately from the body.
- Generate the hash from Canonical JSON (of ignored + masked data).
- Never hash encrypted content.
- Uniqueness: one response per `(RunId, RequestId)` (`UX_Run_Request`).

> **Encryption is planned (not yet implemented).** The business/architecture docs specify
> **encrypted** responses (AES-256) in an `EncryptedResponse` column, but the current schema
> stores the body in `ResponseJson` as plain `nvarchar(MAX)` — only `ResponseHash` exists. Add
> the encryption column when implementing. See `docs/architecture/ImplementationStatus.md`.

---

# Performance

- Load `ResponseJson` only when required (e.g. deep compare); avoid loading bodies for summaries.
- Use `ResponseHash` for quick equality comparison.
