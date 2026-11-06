import { Routes } from '@angular/router';

import { RequestListPageComponent } from '@features/request-pool/pages/request-list-page/request-list-page.component';

import { DatasetListPageComponent } from './pages/dataset-list-page/dataset-list-page.component';

/**
 * Datasets module: the Dataset List, and each dataset's requests nested under it.
 * /projects/:projectId/datasets and /projects/:projectId/datasets/:datasetId/requests.
 */
export const DATASETS_ROUTES: Routes = [
  { path: '', component: DatasetListPageComponent },
  { path: ':datasetId/requests', component: RequestListPageComponent, data: { breadcrumb: 'Requests' } },
];
