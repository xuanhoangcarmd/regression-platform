# Purpose

Keep persistence models hidden behind DTOs and standardize how data is mapped between layers.

# Rules

- Never expose database models or return entities directly.
- Map through layers: Database DTO → Application DTO → Response DTO.
- Prefer manual mapping; avoid AutoMapper unless necessary.

# AI Generation Requirements

When generating code, the AI **must**:

- Map to a Response DTO before returning data.
- Use manual mapping by default.

The AI **must never**:

- Return entities or persistence models directly.
- Introduce AutoMapper without justification.
