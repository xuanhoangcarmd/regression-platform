# Purpose

Define how Angular communicates with the backend APIs through a typed, consistent
data-access layer, keeping HTTP concerns out of components.

# Rules

## HttpClient usage

- `HttpClient` is used only inside API services in `core/apis/` or `features/*/apis/`.
- Never use `HttpClient` in pages, components, pipes, or helpers.
- Configure HTTP with `provideHttpClient(withInterceptors([...]))`; use functional interceptors only.
- Centralize the API base URL and version in configuration/environment; never hardcode URLs in services.

## API service structure & responsibilities

- One service per API domain (e.g. `ProjectApiService`, `CompareApiService`, `RegressionRunApiService`).
- API services **should**: execute HTTP requests, return typed responses, handle request serialization.
- API services **should NOT**: contain business logic, store state, or display notifications.

## Typed models

- Always use typed request/response DTOs that mirror the backend contract; never `any` or `object`.
  - Good: `Observable<ProjectResponse>` — Bad: `Observable<any>`, `Observable<object>`.
- Use dedicated request DTOs (e.g. `CreateRunRequest`, `CompareRunsRequest`) and response DTOs (e.g. `CompareSummaryResponse`, `RunHistoryResponse`).

## Error handling

- Global errors are handled by the HTTP error interceptor.
- Feature-specific errors are handled by the feature service.
- Do not handle HTTP errors inside components.
- Map backend `ProblemDetails` into typed errors and surface user-friendly messages (see notifications rules).

## Data exposure & query params

- Handle auth (JWT bearer) and correlation id in interceptors, not in each service.
- Expose results as signals (`toSignal()`) or observables consumed via the `async` pipe; track loading/error state in the (feature) service.
- Respect pagination/sorting/filtering query params (`pageNumber`, `pageSize`, `sortBy`, `sortDirection`, ...) matching the API contract.

## API flow

```text
Page → Feature Service → API Service → Backend API
```

# AI Generation Requirements

When generating data access, the AI **must**:

- Create dedicated API services in `core/apis` or `features/*/apis` using `inject(HttpClient)` and shared typed DTOs.
- Keep API services focused on communication only; place orchestration in feature services.
- Use interceptors for auth, correlation id, and error handling.

The AI **must never**:

- Inject/call `HttpClient` from pages, components, pipes, or helpers.
- Hardcode API URLs or use `any`/`object` for request/response types.
- Put business logic, state, or notifications inside an API service, or handle HTTP errors in components.
