# Purpose

Expose health endpoints for monitoring the API and its dependencies.

# Rules

- Expose a `/health` endpoint.
- Check: SQL Server, external APIs, AI provider, worker status.
- Keep the health endpoint lightweight.

# AI Generation Requirements

When generating code, the AI **must**:

- Register health checks for the listed dependencies and expose `/health`.

The AI **must never**:

- Add heavy or slow operations to the health endpoint.
