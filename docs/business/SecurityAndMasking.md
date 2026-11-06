# Security and Masking

> **⚠ Encryption planned / not yet implemented.** The current schema stores
> `RunResponse.ResponseJson` as plain text (only `ResponseHash` exists) — there is no
> `EncryptedResponse` column. Masking and hashing apply today; AES-256 encryption is a target.
> See `docs/architecture/ImplementationStatus.md`.

## Purpose

Protect sensitive information while preserving regression testing capabilities.

The platform must never expose confidential data unnecessarily.

---

# Security Goals

* Protect sensitive payloads
* Prevent credential leakage
* Protect customer information
* Support auditing
* Maintain compliance requirements

---

# Sensitive Data

Examples:

* Password
* AccessToken
* RefreshToken
* ApiKey
* SecretKey
* SessionId
* Authorization Header

Sensitive data must never appear in logs.

---

# PII Data

Examples:

* Email
* Phone Number
* Address
* Customer Identifier

Projects may configure additional masking rules.

---

# Masking Process

Masking is part of the capture pipeline, applied after Ignore Rules and before hashing:

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

---

# Masking Example

Original:

```json
{ "email": "john@example.com" }
```

Stored:

```json
{ "email": "***MASKED***" }
```

---

# Header Protection

Never store raw authorization headers.

```text
Original:  Authorization: Bearer xxxxx
Stored:    Authorization: ***MASKED***
```

---

# Logging Rules

Never log:

* Access Tokens
* API Keys
* Passwords
* Secrets

Avoid logging full response bodies. Prefer identifiers:

* RequestId
* RunId
* CompareId

---

# Encryption Rules

* Use AES-256 encryption.
* Keys must be managed externally — e.g. Azure Key Vault, environment variables, secret management service.
* Never commit encryption keys to source control.

---

# Access Control

Only authorized users may:

* View responses
* Download reports
* Access compare results

Future role-based access control should be supported.

---

# Auditing

Record:

* Response creation
* Compare execution
* Report generation

Audit records should not expose sensitive values.

---

# AI Rules

* AI analysis should use masked data.
* AI must never receive passwords, tokens, or secrets.
* AI summaries should be generated from sanitized content only.

---

# AI Generation Requirements

* Mask sensitive fields before persistence.
* Never log secrets.
* Encrypt stored responses.
* Support configurable masking rules.
* Ensure AI only accesses sanitized data.
