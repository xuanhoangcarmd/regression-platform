import { ActivatedRouteSnapshot, Routes } from '@angular/router';

import { ExpectedResponsePageComponent } from './pages/expected-response-page/expected-response-page.component';
import { RunDetailPageComponent } from './pages/run-detail-page/run-detail-page.component';
import { RunListPageComponent } from './pages/run-list-page/run-list-page.component';
import { displayRunId } from './services/run.service';

export const REGRESSION_RUNS_ROUTES: Routes = [
  { path: '', component: RunListPageComponent },
  {
    path: ':runId',
    component: RunDetailPageComponent,
    data: { breadcrumb: (route: ActivatedRouteSnapshot) => displayRunId(Number(route.params['runId'])) },
  },
  {
    path: ':runId/expected',
    component: ExpectedResponsePageComponent,
    // Unsaved drafts → confirm before navigating away.
    canDeactivate: [(page: ExpectedResponsePageComponent) => page.canLeave()],
    data: { breadcrumb: (route: ActivatedRouteSnapshot) => `${displayRunId(Number(route.params['runId']))} · Expected Responses` },
  },
];
