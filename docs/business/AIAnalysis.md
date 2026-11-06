> **⚠ Persistence planned / not yet implemented.** There is no `AIAnalysis` table yet; AI
> summaries are not persisted in the current schema. The rules below (inputs/outputs,
> AI-must-not) still apply when generating summaries. See `docs/architecture/ImplementationStatus.md`.

# Purpose

Generate AI summaries for Compare Results.

---

# Inputs

- Compare Result
- Compare Details

---

# Outputs

- Executive Summary
- Breaking Changes
- Risk Assessment
- Recommendation

---

# AI Must Not

- Invent data.
- Assume missing information.
- Modify compare results.
