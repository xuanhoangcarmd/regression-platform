# Architecture (WHY) — Index

Why the system is designed the way it is. **Read this index first**, pick the document that
matches your task, then read only that file — do not read the whole folder.

| Document | Read when… |
|----------|------------|
| `Architecture.md` | you need the big-picture system view (layers + functional flow) — start here |
| `BackendArchitecture.md` | understanding backend layering, or why CQRS / Dapper / no EF Core |
| `FrontendArchitecture.md` | understanding the Angular core/features/shared split, or when to add a feature |
| `DatabaseArchitecture.md` | understanding the entity flow, why ResponseHash, or why encrypted responses |
| `CompareArchitecture.md` | understanding why compare-by-RequestId, hash-first, deep-compare-second |
| `AIArchitecture.md` | understanding the AI summary pipeline or AI constraints |
| `SecurityArchitecture.md` | understanding the capture pipeline (ignore → mask → canonical → hash → encrypt → store) |
| `DeploymentArchitecture.md` | understanding hosting/deployment topology (IIS now, Azure later) |
| `Decisions.md` | you need the rationale behind a specific decision (ADRs) |
| `ImplementationStatus.md` | a doc mentions a feature you can't find in the schema (implemented vs planned + term mapping) |

Other perspectives: WHAT = `docs/business/`, HOW = `docs/rules/`, DATA = `docs/database/`.
