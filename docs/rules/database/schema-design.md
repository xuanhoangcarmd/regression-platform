# Purpose

Define how tables are structured so the schema is normalized, consistent, and aligned with
the Clean Architecture domain model and the regression platform's immutability rules.

# Rules

- Every table has a surrogate primary key `Id BIGINT IDENTITY(1,1)`.
- Every table includes standard audit columns: `Id`, `CreatedDate`, `CreatedBy`, `UpdatedDate`, `UpdatedBy`, `IsActive`.
- `CreatedDate`/`UpdatedDate` use `DATETIME2` and are set in UTC.
- Use soft deletes via `IsActive` (BIT); do not physically delete business records.
- The schema is multi-project: business tables reference `ProjectId` and queries are scoped by project.
- Choose precise data types:
  - Keys/foreign keys: `BIGINT`
  - Text: `NVARCHAR(n)` with a real length; `NVARCHAR(MAX)` only for large JSON/payloads
  - Hashes (e.g. `ResponseHash`): fixed-length (e.g. `CHAR(64)` for SHA-256 hex)
  - Money/decimals: `DECIMAL(p,s)` — never `FLOAT` for monetary values
  - Booleans: `BIT`
- Normalize to at least 3NF; avoid repeating groups and redundant columns.
- `ApiRequest` is immutable — never update an existing row; insert a new record when a test case changes.
- `RunResponse` is uniquely identified by `(RunId, RequestId)`.
- Store large request/response bodies as `NVARCHAR(MAX)`/JSON; never store encrypted content where it would be hashed.

# AI Generation Requirements

When generating schema, the AI **must**:

- Add `Id BIGINT IDENTITY` plus the standard audit columns to every table.
- Use precise, sized data types and `DATETIME2` UTC timestamps.
- Model `ApiRequest` as append-only and enforce uniqueness of `(RunId, RequestId)`.

The AI **must never**:

- Use `INT`/`GUID` for identity keys without reason, or `FLOAT` for money.
- Generate `UPDATE` statements against `ApiRequest`.
- Physically delete business records instead of using `IsActive`.
