# Purpose

Standardize pagination across all list endpoints.

# Rules

- All list endpoints must support pagination.
- Defaults: `pageNumber = 1`, `pageSize = 20`. Maximum `pageSize = 100`.
- Responses return: `items`, `pageNumber`, `pageSize`, `totalCount`, `totalPages`.

```json
{
  "items": [],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 100,
  "totalPages": 5
}
```

# AI Generation Requirements

When generating code, the AI **must**:

- Add `pageNumber`/`pageSize` parameters with the defaults to list endpoints.
- Return the standard paged response shape.

The AI **must never**:

- Return unbounded lists or allow `pageSize` above 100.
