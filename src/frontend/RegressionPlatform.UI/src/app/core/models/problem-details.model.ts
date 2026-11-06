/**
 * RFC 7807 ProblemDetails returned by the API on error.
 * The API never returns plain-string errors — see docs/rules/dotnet/api-design.md.
 */
export interface IProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  /** Validation errors keyed by field (ASP.NET ValidationProblemDetails). */
  errors?: Record<string, string[]>;
  /** Trace/correlation id echoed by the backend, when present. */
  traceId?: string;
  /** ProblemDetails allows arbitrary extension members. */
  [key: string]: unknown;
}
