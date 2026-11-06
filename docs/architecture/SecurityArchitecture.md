# Security Architecture

> **⚠ Encryption planned / not yet implemented.** The "Encrypt → Store" step is a target; the
> current schema stores `RunResponse.ResponseJson` in plain text (only `ResponseHash` exists).
> Masking + hashing apply today. See `ImplementationStatus.md`.

Capture pipeline (consistent with `docs/business/ResponseStorage.md` — Ignore Rules run
first at capture time, before masking):

```text
Response
    │
    ▼
Ignore Rules
    │
    ▼
Mask
    │
    ▼
Canonical JSON
    │
    ▼
Hash
    │
    ▼
Encrypt
    │
    ▼
Store
```

---

# PII

- Personal data (email, phone, address, customer identifiers) is masked before persistence.
- Projects may configure additional masking rules.
- AI and reports only ever see masked content.

# Tokens / Secrets

- Credentials (passwords, access/refresh tokens, API keys, authorization headers) must never be stored or logged.
- They are masked out during the masking step and replaced with `***MASKED***`.

# Encryption

- Stored responses are encrypted at rest using **AES-256**.
- Encryption happens **after** hashing, so it does not affect the hash or comparison.
- Keys are managed externally (Azure Key Vault, environment variables, secret manager) and never committed to source control.

See `docs/business/SecurityAndMasking.md`, `docs/rules/database/security.md`, and `Decisions.md` (ADR-003).
