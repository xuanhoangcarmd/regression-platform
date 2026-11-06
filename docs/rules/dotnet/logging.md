# Purpose

Provide enough log information for troubleshooting, auditing, performance analysis, and
production monitoring — without exposing sensitive data.

# Rules

- Use Serilog with structured logging.
- Use appropriate levels: Trace, Debug, Information (business events), Warning, Error, Critical.
- Log: app startup/shutdown, regression run start/complete, compare job start/complete, worker execution, unhandled exceptions, external API failures, DB failures.
- Never log: passwords, access/refresh tokens, JWT, connection strings, API secrets, sensitive customer data, large request/response JSON.
- Use message templates with properties, not string interpolation: `LogInformation("Run {RunId} completed in {Duration} ms", runId, duration)`.
- Always log the exception object (`LogError(ex, "...")`); never swallow exceptions silently.
- Propagate a `CorrelationId` through API → Application → Repository → Worker → Logs.
- Log execution time for DB queries, background jobs, external API calls, AI calls, and long-running operations.
- Repositories log only unexpected failures, not successful queries; controllers don't log in detail — global middleware handles request logging.
- For AI: log prompt start/complete, duration, and token usage; never log sensitive prompt content.
- Include identifiers when available: `RunId`, `ProjectId`, `ApiId`, `RequestId`, `CompareId`, `JobId`.

# AI Generation Requirements

When generating code, the AI **must**:

- Use Serilog structured logging with message templates and relevant identifiers.
- Log exceptions with the exception object and flow `CorrelationId` through layers.

The AI **must never**:

- Log secrets, tokens, connection strings, or large request/response payloads.
- Use interpolated log strings or swallow exceptions silently.
