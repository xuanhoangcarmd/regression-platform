import { ActivatedRouteSnapshot, Routes } from '@angular/router';

import { ReportDetailPageComponent } from './pages/report-detail-page/report-detail-page.component';
import { ReportListPageComponent } from './pages/report-list-page/report-list-page.component';
import { displayReportId } from './services/report.service';

export const REPORTS_ROUTES: Routes = [
  { path: '', component: ReportListPageComponent },
  {
    path: ':reportId',
    component: ReportDetailPageComponent,
    data: { breadcrumb: (route: ActivatedRouteSnapshot) => displayReportId(Number(route.params['reportId'])) },
  },
];
