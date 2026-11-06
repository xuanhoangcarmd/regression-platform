# AI Architecture

> **⚠ Persistence planned / not yet implemented.** No `AIAnalysis` table exists yet; summaries
> are generated from the Compare Result but not stored. See `ImplementationStatus.md`.

```text
Compare Result
    │
    ▼
AI Prompt Builder
    │
    ▼
OpenAI
    │
    ▼
AI Summary
```

- **Compare Result** — structured, field-level differences produced by the Compare Engine.
- **AI Prompt Builder** — builds a prompt from the compare result using sanitized (masked) data only.
- **OpenAI** — the model provider (Azure OpenAI optional).
- **AI Summary** — human-readable explanation: change summary, breaking changes, risk assessment, recommendation.

---

# Rules

- AI must **not** infer, assume, or invent data — it summarizes only what the Compare Result contains.
- AI **only summarizes**; it must never modify comparison results.
- AI receives sanitized/masked content only — never passwords, tokens, or secrets.

See `docs/business/AIAnalysis.md` and `docs/business/SecurityAndMasking.md`.
