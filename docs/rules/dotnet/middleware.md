# Purpose

Centralize cross-cutting concerns in global middleware so controllers stay lightweight.

# Rules

- Global middleware handles: exception handling, CorrelationId, request logging, response logging, authentication, and performance timing.
- Controllers must remain lightweight and free of these concerns.

# AI Generation Requirements

When generating code, the AI **must**:

- Implement cross-cutting concerns in middleware.

The AI **must never**:

- Duplicate exception handling, logging, or timing logic inside controllers.
