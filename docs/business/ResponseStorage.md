# Response Storage

> **⚠ Encryption planned / not yet implemented.** The current schema stores `ResponseJson` as
> plain `nvarchar(MAX)` with `ResponseHash` (CHAR(64)); there is no `EncryptedResponse` column.
> The "Encrypt Response" step below is a target. See `docs/architecture/ImplementationStatus.md`.

## Purpose

Define how API responses are stored, protected, indexed, and retrieved for regression comparison.

The system must support:

* Large-scale response storage
* Fast comparison
* Historical auditing
* Data security
* AI analysis

---

# Response Lifecycle

```text
Response Received
        │
        ▼
Apply Ignore Rules
        │
        ▼
Apply Masking Rules
        │
        ▼
Canonical JSON
        │
        ▼
Response Hash
        │
        ▼
Encrypt Response
        │
        ▼
Store Database
```

Order rationale:

* **Ignore Rules first** — ignored fields directly affect comparison, so they are removed before anything else.
* **Masking next** — guarantees no PII/tokens are ever persisted.
* **Canonical JSON + Hash** — the hash is generated from already-normalized and masked data, giving stable comparison across sprints.
* **Encryption last** — encryption only protects data at rest; it does not affect the hash or comparison.

---

# Storage Strategy

Each request execution generates one response record.

A response record contains:

* RunId
* RequestId
* StatusCode
* Duration
* ResponseHash
* EncryptedResponse
* ResponseSize
* CreatedDate

---

# Response Format

* Responses must be stored as JSON.
* XML responses should be converted to JSON when possible.
* Response formatting must not affect comparison results.

---

# Canonical JSON

Before generating hashes, responses must be converted to Canonical JSON.

Canonical JSON removes:

* Formatting differences
* Indentation
* Property ordering differences

Example — both inputs must produce identical canonical output:

```json
{ "name": "John", "age": 30 }
```

```json
{ "age": 30, "name": "John" }
```

---

# Encryption

* Responses must be encrypted before storage, using **AES-256**.
* Encryption occurs before persistence.
* Encryption keys must never be stored inside source code.

---

# Response Hash

* Hash must be generated from Canonical JSON.
* Hash must be generated **before** encryption — encryption may produce different ciphertext for identical payloads.
* Recommended algorithm: **SHA-256**.

---

# Duplicate Detection

ResponseHash may be used for:

* Quick comparison
* Duplicate detection
* Change detection

Hash equality does not require Deep Compare.

---

# Retrieval

When retrieving a response:

```text
Load EncryptedResponse
        ↓
Decrypt
        ↓
Deserialize
        ↓
Process
```

---

# Data Retention

* Responses may be retained according to project configuration.
* Recommended: 12 months minimum.
* Support future archival strategy.

---

# Performance Considerations

* Response bodies may exceed several megabytes.
* Do not deserialize responses unless required.
* Compare hashes before performing deep comparison.
* Avoid loading response bodies for summary reports.

---

# AI Generation Requirements

* Store encrypted responses.
* Store response hash separately.
* Generate hash from canonical JSON.
* Never generate hash from encrypted content.
* Support efficient retrieval by RunId and RequestId.
