# Purpose

Define how Swagger / OpenAPI documentation must be written and maintained for the
ASP.NET Core Web API in this project. The goal is to keep API documentation accurate,
complete, and always synchronized with the actual implementation so that consumers
(frontend, QA, external teams) can rely on it as the single source of truth.

# Rules

- Every endpoint must be documented with:
  - **Summary** — a short, one-line description of what the endpoint does.
  - **Description** — a longer explanation of behavior, side effects, and constraints.
  - **`[ProducesResponseType]`** — declared for every possible status code the endpoint can return (e.g. `200`, `201`, `400`, `404`, `409`, `500`).
  - **Request example** — a representative example of the request body / parameters.
  - **Response example** — a representative example of the response payload.
- Error responses must be documented as `ProblemDetails`, consistent with the API error contract.
- Swagger UI must be enabled in the **Development** environment, and must not be exposed in Production unless explicitly required and secured.
- XML documentation comments (`///`) on controllers, actions, and DTOs must be enabled and included in the Swagger generation pipeline.
- Documentation must stay synchronized with the implementation: whenever an endpoint, request, response, or status code changes, its Swagger annotations must be updated in the same change.
- Use consistent operation IDs and group endpoints by tag (feature/controller) so the generated spec is organized.
- Do not document or expose internal-only or deprecated endpoints without clearly marking them (`[Obsolete]` / deprecated tag).

# AI Generation Requirements

When generating or modifying API endpoints, the AI **must**:

- Add `[ProducesResponseType]` attributes for all success and error status codes the action can produce.
- Provide Summary and Description via XML doc comments (`/// <summary>`, `/// <remarks>`) or Swagger annotations.
- Include request and response examples (e.g. via `[SwaggerRequestExample]` / `[SwaggerResponseExample]` or example schema attributes).
- Reference `ProblemDetails` as the response type for error cases.
- Update existing Swagger annotations whenever the endpoint signature, DTO, or status codes change.

The AI **must never**:

- Generate an endpoint without `[ProducesResponseType]` declarations.
- Leave Summary, Description, or examples empty or as placeholders.
- Fabricate example data that does not match the real DTO shape — examples must reflect actual request/response models.
- Document a status code or response shape the endpoint does not actually return.
- Enable Swagger UI unconditionally in Production.
- Let documentation drift out of sync with the implementation.
