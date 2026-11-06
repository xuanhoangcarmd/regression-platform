# Purpose

Define how access is controlled, keeping authorization separate from business logic.

# Rules

- Prefer policy-based authorization; controllers use `[Authorize(Policy = "...")]`.
- Avoid role checks inside business logic.
- Business rules must remain independent of authorization.

# AI Generation Requirements

When generating code, the AI **must**:

- Apply authorization via policies on controllers/endpoints.

The AI **must never**:

- Embed role/authorization checks inside business logic.
