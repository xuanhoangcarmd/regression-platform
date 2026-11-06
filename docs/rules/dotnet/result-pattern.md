# Purpose

Use the `Result<T>` pattern for business outcomes instead of throwing exceptions, making
success and failure explicit and predictable.

# Rules

- Business operations return `Result<T>`, not thrown exceptions.
- Standard result states: Success, Failure, ValidationFailure, NotFound, Conflict, Unauthorized, Forbidden.
- Do not throw exceptions for business validation; use exceptions only for unexpected system failures.
- Handler return types should be strongly typed (e.g. `Task<Result<CreateProjectResponse>>`).
- Avoid `Task<object>`, `Task<dynamic>`, and `Task<bool>` as handler return types.

# AI Generation Requirements

When generating code, the AI **must**:

- Return `Result<T>` with the appropriate result state for business outcomes.
- Use strongly typed result responses.

The AI **must never**:

- Throw exceptions for business/validation failures.
- Return `object`, `dynamic`, or bare `bool` from handlers.
