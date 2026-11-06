# Purpose

Standardize database object names so the SQL Server schema is consistent, predictable, and
easy to map to domain entities and Dapper DTOs.

# Rules

- Tables use **singular** PascalCase names (actual: `Api`, `ApiRequest`, `Run`, `RunResponse`, `CompareJob`, `CompareResult`, `CompareDetail`, `CompareIgnoreRule`).
- Columns use PascalCase (e.g. `ProjectId`, `ResponseHash`, `CreatedDate`).
- Primary key column is always `Id`.
- Foreign key columns are `<ReferencedTable>Id` (e.g. `ProjectId`, `RunId`, `RequestId`).
- Standard audit columns on every table: `Id`, `CreatedDate`, `CreatedBy`, `UpdatedDate`, `UpdatedBy`, `IsActive`.
- Boolean columns use an `Is`/`Has` prefix (e.g. `IsActive`, `HasBreakingChange`).
- Constraint and index naming:
  - Primary key: `PK_<Table>`
  - Foreign key: `FK_<Table>_<ReferencedTable>`
  - Unique: `UQ_<Table>_<Columns>`
  - Index: `IX_<Table>_<Columns>`
  - Check: `CK_<Table>_<Rule>`
  - Default: `DF_<Table>_<Column>`
- No spaces, Hungarian notation, abbreviations, or reserved keywords as object names.
- SQL parameters are PascalCase with `@` (e.g. `@ProjectId`, `@RunId`).

# AI Generation Requirements

When generating schema or SQL, the AI **must**:

- Use singular PascalCase table names and the standard audit columns.
- Name keys, constraints, and indexes using the patterns above.
- Name foreign keys as `<ReferencedTable>Id`.

The AI **must never**:

- Pluralize table names or invent ad-hoc constraint/index names.
- Use abbreviations, spaces, or reserved keywords in object names.
