import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, shareReplay } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ComboOption } from '../components/combo-select';

export interface OemEnumEntry extends ComboOption {
  makeId: number;
}

export interface OemSystemEnumsRaw {
  makes: ComboOption[];
  system: OemEnumEntry[];
  subsystem: OemEnumEntry[];
}

/** Loads OEM make/system/subsystem enum lists (per-make System/SubSystem for OEM DTCs). */
@Injectable({ providedIn: 'root' })
export class OemSystemEnumsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.payloadGenApiBaseUrl;

  private cache$?: Observable<OemSystemEnumsRaw>;

  getAll(): Observable<OemSystemEnumsRaw> {
    if (!this.cache$) {
      this.cache$ = this.http
        .get<OemSystemEnumsRaw>(`${this.baseUrl}/oem-system-enums`)
        .pipe(shareReplay(1));
    }
    return this.cache$;
  }
}
