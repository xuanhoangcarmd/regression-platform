/**
 * Application route paths (kebab-case, aligned with feature folders — routing.md).
 * Reference these instead of hardcoding navigation strings.
 */
export const RouteConstants = {
  dashboard: 'dashboard',
  projects: 'projects',
  dataLibrary: 'data-library',
  apiDefinitions: 'api-definitions',
  requestPool: 'request-pool',
  regressionRuns: 'regression-runs',
  compare: 'compare',
  reports: 'reports',
  settings: 'settings',
} as const;

/**
 * Per-project module sub-paths under /projects/:projectId/...
 * Multi-project is PLANNED — see docs/architecture/ImplementationStatus.md.
 */
export const ProjectRouteConstants = {
  dashboard: 'dashboard',
  apis: 'apis',
  datasets: 'datasets',
  requestPool: 'request-pool',
  runs: 'runs',
  compare: 'compare',
  reports: 'reports',
  ignoreRules: 'ignore-rules',
  configurations: 'configurations',
} as const;
