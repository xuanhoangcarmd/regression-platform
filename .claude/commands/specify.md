---
description: "Create or update a feature specification from a natural language description (Regression Platform)."
argument-hint: "Describe the feature you want to specify"
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding. If it is empty, ERROR: "No feature description provided".

## Mandatory Rules

This project's authoritative guidance lives in `CLAUDE.md` and `docs/`. A specification is a
**business/WHAT** document, so:

- Read `docs/business/README.md` first, then only the matching business doc(s) for this feature
  (the "Read when…" catalog). Reuse existing domain terms — see `docs/business/Terminology.md`.
- Use the **real schema as the source of truth** when naming entities: `docs/database/Tables.md`.
  Any capability not yet in the schema MUST be flagged **(Planned)** per
  `docs/architecture/ImplementationStatus.md` (no multi-project `Project` table, no response
  encryption, no AI-summary persistence, no `CreatedBy`/`UpdatedBy`).
- Do **not** read the HOW rules (`docs/rules/**`) here — those are for `/implement`.
- A spec describes WHAT and WHY only. Do **not** include tech stack, APIs, SQL, or code structure.

## Outline

The text the user typed after `/specify` **is** the feature description. Do not ask the user to
repeat it unless it is empty.

1. **Generate a concise short name** (2–4 words, kebab-case, action-noun when possible):
   - "Compare two runs side by side" → `compare-runs-view`
   - "Add masking rules for sensitive fields" → `masking-rules`
   - "Dashboard for regression trends" → `regression-dashboard`

2. **Resolve the feature directory** under `specs/`:
   - Scan existing `specs/` directories for the highest 3-digit prefix `NNN`; the new prefix is the next number (start at `001`).
   - Directory name: `specs/<NNN>-<short-name>` → this is `FEATURE_DIR`.

3. **Create the spec**:
   - `mkdir -p FEATURE_DIR`.
   - Copy `.specify/templates/spec-template.md` to `FEATURE_DIR/spec.md` → this is `SPEC_FILE`.
   - Persist a pointer so `/implement` can find this feature without arguments — write `.specify/feature.json`:
     ```json
     { "feature_directory": "specs/<NNN>-<short-name>" }
     ```
     Write the actual resolved path, not the literal placeholder.

4. **Fill the spec** from the description, following this flow:
   1. Parse the description → identify actors, actions, data, constraints.
   2. For unclear aspects, make informed guesses from context and this project's domain. Only mark
      `[NEEDS CLARIFICATION: question]` when no reasonable default exists. **Max 3 markers**, prioritized: scope > security/privacy > UX > technical detail.
   3. Fill **User Scenarios & Testing** (primary flow + acceptance scenarios + edge cases).
   4. Write **Functional Requirements** — each testable and unambiguous.
   5. Define **Key Entities**, mapping to real `RegressionDB` tables; flag **(Planned)** entities.
   6. Define **Success Criteria** — measurable and technology-agnostic.
   7. Record **Assumptions** for any defaults you applied.
   - Preserve the template's section order and headings; remove sections that don't apply.

5. **Validate quality** before finishing. Check the spec against:
   - No implementation details (no languages, frameworks, APIs, SQL).
   - Every functional requirement is testable and unambiguous.
   - Success criteria are measurable and technology-agnostic.
   - Acceptance scenarios and edge cases are present; scope is bounded.
   - Entities map to the real schema or are flagged **(Planned)**.

   If items fail (excluding `[NEEDS CLARIFICATION]`), fix the spec and re-check (max 3 passes).
   If `[NEEDS CLARIFICATION]` markers remain, present each as a short question with 2–3 suggested
   answers in a markdown table and wait for the user's choice before finalizing.

6. **Report completion**:
   - `FEATURE_DIR` and `SPEC_FILE` paths.
   - A one-line quality summary and any remaining clarifications.
   - Next step: run `/implement` to build it.
