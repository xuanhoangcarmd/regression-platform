# Implementation Status

Tracks which documented capabilities exist in the **current** `RegressionDB` schema versus
which are **planned (not yet implemented)**. The `docs/database/` (DATA) docs always reflect
the current schema; business/architecture docs may describe planned features marked as such.

> Read this when a business/architecture doc mentions a feature you cannot find in the schema.

---

# Implemented (current schema)

| Capability | Where |
|------------|-------|
| API definitions | `Api` table |
| Versioned test requests | `ApiRequest` (`Version`, `IsActive`) |
| Regression runs | `Run` |
| Response capture + hashing | `RunResponse` (`ResponseJson`, `ResponseHash` CHAR(64)) |
| Async compare jobs | `CompareJob` (status/progress) |
| Compare summary | `CompareResult` (Run A vs Run B counts) |
| Per-request diff | `CompareDetail` (`Result`, `DiffJson`) |
| Regression reports (Phase 1: list + Summary/stats) | `Report` (1 report = 1 compare; snapshot counts + computed `RegressionScore`/`RiskLevel`) |
| Ignore rules | `CompareIgnoreRule` (`JsonPath`, `IgnoreOrder`, `Enabled`) |
| Per-project environment config | `Project`, `ProjectConfiguration` (Base URL per env), `ProjectConfigurationHeader` (headers, secret-aware) |
| Project-scoped core data | `Api`, `Run`, `CompareIgnoreRule` have `ProjectId` (children inherit via parent). Endpoints scope by `?projectId={code}`. `Api.Name` unique per project. |

---

# Planned (NOT yet implemented)

| Feature | Documented in | Status in schema |
|---------|---------------|------------------|
| **Multi-project UI** (project switcher, per-project navigation) | `business/Introduction.md`, `business/ProjectManagement.md`, CLAUDE.md | Data is project-scoped (below); a project-switcher UI is not built yet (routes hardcode `oreilly`) |
| **Response encryption** (AES-256, `EncryptedResponse`) | `business/SecurityAndMasking.md`, `business/ResponseStorage.md`, `architecture/SecurityArchitecture.md` | `ResponseJson` stored as plain `nvarchar(MAX)`; only `ResponseHash` exists |
| **AI summary persistence** (AIAnalysis entity) | `business/AIAnalysis.md`, `architecture/AIArchitecture.md` | No `AIAnalysis` table |
| **Full audit** (`CreatedBy`, `UpdatedBy`) | `docs/rules/database/*` (target standard) | Only `CreatedDate` (+ `UpdatedDate` on Api); no `CreatedBy`/`UpdatedBy` |
| **Run environment** (DEV/QA/UAT/PROD per run) | `business/RegressionRun.md`, `business/ProjectManagement.md` | `Run` has no `Environment` column |
| **Report AI summary / export (PDF/Excel/CSV) / email / share / activity** | `business/Reports.md`, report page mockups | `Report` table exists (Phase 1); these later phases are not implemented (no AI/SMTP/export services) |

When implementing any of these, update the schema first, then move the row to "Implemented"
and remove the "Planned" banners from the relevant docs.

---

# Terminology mapping (business concept → actual table)

| Business / docs term | Actual table / column |
|----------------------|-----------------------|
| ApiDefinition | `Api` |
| Request / RequestPool | `ApiRequest` |
| RegressionRun | `Run` |
| RunResponse / RunResult | `RunResponse` |
| Compare / CompareRuns | `CompareResult` (+ `CompareJob` for async) |
| CompareDetail (JsonPath/ChangeType/OldValue/NewValue) | `CompareDetail` (`Result` + `DiffJson`) |
| IgnoreRule | `CompareIgnoreRule` |
| Report / Regression Report | `Report` (1:1 with `CompareJob`) |
| Project | `Project` (config scope only for now) |
| Environment configuration / Headers | `ProjectConfiguration` / `ProjectConfigurationHeader` |
| Baseline / Target run | `RunAId` / `RunBId` |
