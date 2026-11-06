# Purpose

Standardize where configuration and secrets come from, keeping secrets out of source code.

# Rules

- Configuration sources: `appsettings.json`, environment variables, User Secrets (Development), Azure Key Vault (future).
- Never hardcode connection strings, API keys, or secrets.

# AI Generation Requirements

When generating code, the AI **must**:

- Read configuration via `IConfiguration`/options from the approved sources.

The AI **must never**:

- Hardcode connection strings, API keys, or secrets.
