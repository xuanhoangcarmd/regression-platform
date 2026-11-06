/**
 * API endpoint path segments (appended to environment.apiBaseUrl in API services).
 * Routes are plural nouns under /api — see docs/rules/dotnet/api-design.md.
 * Never hardcode URLs in services; compose them from these segments.
 */
export const ApiConstants = {
  projects: 'projects',
  apiDefinitions: 'api-definitions',
  datasets: 'datasets',
  requestPool: 'request-pool',
  regressionRuns: 'regression-runs',
  compare: 'compare',
  reports: 'reports',
} as const;
