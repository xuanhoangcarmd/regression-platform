# Purpose

Define how the API authenticates requests securely.

# Rules

- Preferred scheme: JWT Bearer. Future support: Azure AD, OIDC.
- Read all auth configuration from `IConfiguration`; never hardcode secrets.
- Never write JWTs to logs.

# AI Generation Requirements

When generating code, the AI **must**:

- Use JWT Bearer authentication and read settings from `IConfiguration`.

The AI **must never**:

- Hardcode secrets or log JWTs/tokens.
