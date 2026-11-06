# Backend Architecture

```text
API Layer
    │
    ▼
Application Layer
    │
    ▼
Repository
    │
    ▼
SQL Server
```

- **API Layer** — controllers only receive the request, call MediatR, and return the response.
- **Application Layer** — CQRS commands/queries, handlers, FluentValidation; the business workflows live here.
- **Repository** — data access with Dapper; executes SQL and maps results.
- **SQL Server** — relational storage.

---

# Key Decisions

- **CQRS** — commands (writes) and queries (reads) are separated, keeping handlers small and focused.
- **Dapper** — used as the primary data access library for performance and full SQL control.
- **No EF Core** — Entity Framework is not used; the platform handles large response datasets where explicit SQL and lightweight mapping perform better.

See `Decisions.md` (ADR-001) for the Dapper-over-EF rationale.
