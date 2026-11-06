import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, shareReplay } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ComboOption } from '../components/combo-select';

interface VinEnumsRaw {
  manufacturer: ComboOption[];
  year: ComboOption[];
  make: ComboOption[];
  model: ComboOption[];
  trim: ComboOption[];
  engine: ComboOption[];
  transmission: ComboOption[];
  manufacturerMake: Record<string, number>;
}

/** Loads the VIN-profile enum lists (Manufacturer/Year/Make/Model/Trim/Engine/Transmission). */
@Injectable({ providedIn: 'root' })
export class VinEnumsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.payloadGenApiBaseUrl;

  private cache$?: Observable<VinEnumsRaw>;

  getAll(): Observable<VinEnumsRaw> {
    if (!this.cache$) {
      this.cache$ = this.http.get<VinEnumsRaw>(`${this.baseUrl}/vin-enums`).pipe(shareReplay(1));
    }
    return this.cache$;
  }
}
