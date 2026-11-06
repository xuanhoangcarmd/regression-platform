import { ActivatedRouteSnapshot, Routes } from '@angular/router';

import { CompareDetailPageComponent } from './pages/compare-detail-page/compare-detail-page.component';
import { CompareListPageComponent } from './pages/compare-list-page/compare-list-page.component';
import { displayCompareId } from './services/compare.service';

export const COMPARE_ROUTES: Routes = [
  { path: '', component: CompareListPageComponent },
  {
    path: ':compareId',
    component: CompareDetailPageComponent,
    data: { breadcrumb: (route: ActivatedRouteSnapshot) => displayCompareId(Number(route.params['compareId'])) },
  },
];
