# Table Names

Singular PascalCase. Actual tables:

- `Api`
- `ApiRequest`
- `Run`
- `RunResponse`
- `CompareJob`
- `CompareResult`
- `CompareDetail`
- `CompareIgnoreRule`

---

# Primary Keys

`Id` on every table.

- `int` IDENTITY for `Api` and `CompareIgnoreRule`.
- `bigint` IDENTITY for the high-volume tables (`ApiRequest`, `Run`, `RunResponse`, `CompareJob`, `CompareResult`, `CompareDetail`).

---

# Foreign Keys

`<Entity>Id`. Examples in the actual schema:

- `ApiId` (ApiRequest → Api)
- `RunId` (RunResponse → Run)
- `RequestId` (RunResponse / CompareDetail → ApiRequest)
- `CompareResultId` (CompareDetail → CompareResult)
- `RunAId`, `RunBId` (CompareResult / CompareJob → Run)

FK constraints are named `FK_<Child>_<Parent>` (e.g. `FK_RunResponse_Run`).

---

# Audit Columns

The schema uses a lightweight audit set (no `CreatedBy`/`UpdatedBy`):

- `CreatedDate datetime2 DEFAULT getutcdate()` — on every table.
- `UpdatedDate datetime2 NULL` — on `Api` only.
- `IsActive bit DEFAULT 1` — on `Api` and `ApiRequest` only.

JSON payload columns use the `Json` suffix (`HeadersJson`, `QueryStringJson`, `ResponseJson`, `DiffJson`).
