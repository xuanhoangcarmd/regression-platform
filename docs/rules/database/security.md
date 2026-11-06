# Purpose

Protect sensitive data at the database layer and prevent secrets or credentials from being
persisted in plain text.

# Rules

- Never store access tokens, refresh tokens, JWTs, passwords, or API secrets in the database.
- Mask or strip sensitive fields from request/response payloads before persistence.
- Encrypt sensitive-at-rest data where required; never hash encrypted content (`ResponseHash` is computed from Canonical JSON, not encrypted bytes).
- Use parameterized SQL everywhere; never build SQL by string concatenation (prevents injection).
- Grant least-privilege database accounts to the application; avoid `db_owner`/`sa` for app connections.
- Store connection strings and credentials in approved configuration sources (see configuration rules), never in the schema, scripts, or source control.
- Audit-sensitive changes via `CreatedDate`/`UpdatedDate` and, where required, dedicated audit tables.

# AI Generation Requirements

When generating schema or SQL, the AI **must**:

- Use parameterized queries and assume least-privilege access.
- Mask/exclude sensitive fields before they are stored.

The AI **must never**:

- Create columns for or persist tokens, passwords, or secrets in plain text.
- Hash encrypted content or concatenate user input into SQL.
- Embed connection strings or credentials in scripts.
