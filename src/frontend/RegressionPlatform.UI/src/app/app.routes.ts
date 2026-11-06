import { Routes } from '@angular/router';

import { RouteConstants } from '@core/constants';

export const routes: Routes = [
  {
    // App shell (sidebar + content). Feature routes render inside its outlet.
    path: '',
    loadComponent: () => import('@shared/layout/main-layout/main-layout.component').then((m) => m.MainLayoutComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: RouteConstants.dashboard },
      {
        path: RouteConstants.dashboard,
        loadChildren: () => import('@features/dashboard/dashboard.routes').then((m) => m.DASHBOARD_ROUTES),
        data: { breadcrumb: 'Dashboard' },
      },
      {
        // Per-project modules: /projects/:projectId/<module>
        path: RouteConstants.projects,
        loadChildren: () => import('@features/projects/projects.routes').then((m) => m.PROJECTS_ROUTES),
      },
      {
        // Global Data Library (not project-scoped): /data-library
        path: RouteConstants.dataLibrary,
        loadChildren: () => import('@features/data-library/data-library.routes').then((m) => m.DATA_LIBRARY_ROUTES),
        data: { breadcrumb: 'Data Library' },
      },
      {
        path: RouteConstants.settings,
        loadComponent: () =>
          import('@features/settings/pages/settings-page/settings-page.component').then(
            (m) => m.SettingsPageComponent,
          ),
        data: { breadcrumb: 'Settings' },
      },
      // Other top-level features are lazy-loaded the same way once their pages/routes exist:
      //   { path: RouteConstants.apiDefinitions,  loadChildren: () => import('@features/api-definitions/api-definitions.routes').then(m => m.API_DEFINITIONS_ROUTES) },
      //   { path: RouteConstants.requestPool,     loadChildren: () => import('@features/request-pool/request-pool.routes').then(m => m.REQUEST_POOL_ROUTES) },
      //   { path: RouteConstants.regressionRuns,  loadChildren: () => import('@features/regression-runs/regression-runs.routes').then(m => m.REGRESSION_RUNS_ROUTES) },
      //   { path: RouteConstants.compare,         loadChildren: () => import('@features/compare/compare.routes').then(m => m.COMPARE_ROUTES) },
      //   { path: RouteConstants.reports,         loadChildren: () => import('@features/reports/reports.routes').then(m => m.REPORTS_ROUTES) },
    ],
  },
  {
    path: '**',
    loadComponent: () =>
      import('@shared/layout/not-found/not-found-page.component').then((m) => m.NotFoundPageComponent),
  },
];
