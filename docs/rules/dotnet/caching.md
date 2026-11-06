# Purpose

Define when and how to cache to improve performance without risking stale or incorrect data.

# Rules

- Use `IMemoryCache` for local caching; Redis support is planned for the future.
- Suitable cache targets: project list, API definitions, ignore rules.
- Do not cache mutable business operations.
- Always define an expiration for cached entries.

# AI Generation Requirements

When generating code, the AI **must**:

- Use `IMemoryCache` with an explicit expiration, only for stable read data.

The AI **must never**:

- Cache mutable business operations or create cache entries without expiration.
