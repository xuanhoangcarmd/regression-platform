import { Routes } from '@angular/router';

import { DataLibraryPageComponent } from './pages/data-library-page/data-library-page.component';

/** Global Data Library: /data-library (search payloads/raw buffers by YMME / DTC / MIL). */
export const DATA_LIBRARY_ROUTES: Routes = [{ path: '', component: DataLibraryPageComponent }];
