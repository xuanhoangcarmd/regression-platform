# Purpose

Ensure data integrity is enforced at the database level through explicit constraints,
not solely in application code.

# Rules

- Every table has a primary key (`PK_<Table>` on `Id`).
- Every relationship is enforced with a foreign key (`FK_<Table>_<ReferencedTable>`).
- Choose explicit FK delete behavior: prefer `NO ACTION`/soft delete for business data; use `CASCADE` only for owned child rows (e.g. `CompareDetail` under `CompareResult`).
- Enforce business uniqueness with unique constraints/indexes (e.g. unique `(RunId, RequestId)` on `RunResponse`).
- Use `NOT NULL` for all required columns; make nullability intentional.
- Use `DEFAULT` constraints for `CreatedDate` (UTC now) and `IsActive` (`1`).
- Use `CHECK` constraints for enumerable/range values (e.g. status columns, non-negative numbers).
- Name all constraints per the naming rules (`PK_`, `FK_`, `UQ_`, `CK_`, `DF_`).

# AI Generation Requirements

When generating schema, the AI **must**:

- Declare PK, FK, NOT NULL, unique, default, and check constraints explicitly with named constraints.
- Add the unique constraint on `RunResponse (RunId, RequestId)`.
- Choose FK delete behavior deliberately.

The AI **must never**:

- Rely only on application validation for integrity that the DB can enforce.
- Use `CASCADE` deletes on independent business entities.
- Leave constraints unnamed.
