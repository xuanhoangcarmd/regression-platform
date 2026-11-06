import { Routes } from '@angular/router';

import { ProjectRouteConstants } from '@core/constants';
import { PlaceholderPageComponent } from '@shared/components/placeholder-page/placeholder-page.component';

/**
 * Per-project module routes: /projects/:projectId/<module>.
 * Multi-project is PLANNED (no Project table yet); every module currently renders
 * the temporary placeholder page. Swap each `component` for the real page as it's built.
 */
export const PROJECTS_ROUTES: Routes = [
  {
    path: ':projectId',
    children: [
      { path: '', pathMatch: 'full', redirectTo: ProjectRouteConstants.dashboard },
      { path: ProjectRouteConstants.dashboard, component: PlaceholderPageComponent, data: { title: 'Dashboard' } },
      {
        path: ProjectRouteConstants.apis,
        loadChildren: () =>
          import('@features/api-definitions/api-definitions.routes').then((m) => m.API_DEFINITIONS_ROUTES),
        data: { breadcrumb: 'APIs' },
      },
      {
        path: ProjectRouteConstants.datasets,
        loadChildren: () => import('@features/datasets/datasets.routes').then((m) => m.DATASETS_ROUTES),
        data: { breadcrumb: 'Datasets' },
      },
      {
        path: ProjectRouteConstants.runs,
        loadChildren: () =>
          import('@features/regression-runs/regression-runs.routes').then((m) => m.REGRESSION_RUNS_ROUTES),
        data: { breadcrumb: 'Runs' },
      },
      {
        path: ProjectRouteConstants.compare,
        loadChildren: () => import('@features/compare/compare.routes').then((m) => m.COMPARE_ROUTES),
        data: { breadcrumb: 'Compare' },
      },
      {
        path: ProjectRouteConstants.reports,
        loadChildren: () => import('@features/reports/reports.routes').then((m) => m.REPORTS_ROUTES),
        data: { breadcrumb: 'Reports' },
      },
      {
        path: ProjectRouteConstants.ignoreRules,
        loadChildren: () => import('@features/ignore-rules/ignore-rules.routes').then((m) => m.IGNORE_RULES_ROUTES),
        data: { breadcrumb: 'Ignore Rules' },
      },
      {
        path: ProjectRouteConstants.configurations,
        loadChildren: () =>
          import('@features/configurations/configurations.routes').then((m) => m.CONFIGURATIONS_ROUTES),
        data: { breadcrumb: 'Configurations' },
      },
    ],
  },
];
