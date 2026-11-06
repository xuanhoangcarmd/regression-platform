# Purpose

Ensure all APIs follow RESTful principles and are consistent, predictable, versionable,
and easy to consume and maintain.

# Rules

- All endpoints start with `/api` (e.g. `/api/projects`, `/api/api-definitions`, `/api/regression-runs`).
- Use the correct HTTP verb: `GET` (read), `POST` (create), `PUT` (replace), `PATCH` (partial update), `DELETE` (delete).
- Resource names use plural nouns (`/api/projects`), not verbs (`/api/getProject`).
- Controllers end with `Controller` (e.g. `ProjectsController`).
- Use dedicated, immutable request DTOs and response DTOs — never expose entities directly.
- List endpoints support pagination (`pageNumber`, `pageSize`), sorting (`sortBy`, `sortDirection`), and filtering via query params.
- Use correct status codes: `200`, `201`, `204`, `400`, `401`, `403`, `404`, `409`, `500`.
- Return `ProblemDetails` for errors — never plain strings.
- Validation via FluentValidation; no manual validation in controllers.
- Controllers only receive the request, call MediatR, and return the response — no business logic.
- Every endpoint documented in Swagger (summary, description, response types), kept up to date.
- Design for future versioning (`/api/v1/...`); do not hardcode versions unless required.

# AI Generation Requirements

When generating code, the AI **must**:

- Use plural resource routes under `/api`, correct verbs, and correct status codes.
- Use request/response DTOs and return `ProblemDetails` for errors.
- Add pagination/sorting/filtering to list endpoints and Swagger docs to every endpoint.

The AI **must never**:

- Expose or return entities directly.
- Put business logic or manual validation in controllers.
- Return plain-string errors.
