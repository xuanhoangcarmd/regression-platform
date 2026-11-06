import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, of, shareReplay } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ComboOption } from '../components/combo-select';

export interface OFMItemDef {
  makeId: number;
  itemId: number;
  itemName: string;
  itemDescriptionEnum: number;
  unit?: string;
}

/** TPMS Status variant: tire-pressure base items get a sibling with bit 0x8000 set
 * and " Status" appended. Mirrors DLL PID.cs EnumTireStatus (lines 33-52) —
 * descriptionEnum mapping is fixed by the DLL, not per-make. */
const TPMS_STATUS_ENUM: Record<string, number> = {
  'Left Front Tire Pressure Status': 1099,
  'Left Rear Tire Pressure Status': 1100,
  'Right Front Tire Pressure Status': 1101,
  'Right Rear Tire Pressure Status': 1102,
  'Spare Tire Pressure Status': 1120,
  'Tire 1 Pressure Status': 1122,
  'Tire 2 Pressure Status': 1123,
  'Tire 3 Pressure Status': 1124,
  'Tire 4 Pressure Status': 1125,
  'Tire 5 Pressure Status': 1145,
};

/** Loads OFM item definitions per make, augmented with runtime-only TPMS Status variants. */
@Injectable({ providedIn: 'root' })
export class OFMItemsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.payloadGenApiBaseUrl;

  private cache$?: Observable<OFMItemDef[]>;

  getAll(): Observable<OFMItemDef[]> {
    if (!this.cache$) {
      this.cache$ = this.http
        .get<OFMItemDef[]>(`${this.baseUrl}/ofm-items`)
        .pipe(
          catchError(() => of([] as OFMItemDef[])),
          shareReplay(1),
        );
    }
    return this.cache$;
  }

  /** Build combo options for a given makeId. ComboSelect renders id + name itself —
   * we return just itemName as `name` (no manual [id] prefix). Augments DB rows with
   * TPMS Status variants (base | 0x8000, name + " Status") generated at runtime by
   * the DLL and not stored in ofm_itemid. */
  getOptionsForMake(items: OFMItemDef[], makeId: number): ComboOption[] {
    const base = items.filter((d) => d.makeId === makeId);
    const baseOptions: ComboOption[] = base.map((d) => ({ id: d.itemId, name: d.itemName }));

    const tpmsVariants: ComboOption[] = [];
    const seenIds = new Set<number>();
    for (const d of base) {
      const statusName = `${d.itemName} Status`;
      if (!(statusName in TPMS_STATUS_ENUM)) continue;
      const variantId = d.itemId | 0x8000;
      if (seenIds.has(variantId)) continue;
      seenIds.add(variantId);
      tpmsVariants.push({ id: variantId, name: statusName });
    }

    return [...baseOptions, ...tpmsVariants].sort((a, b) => a.name.localeCompare(b.name));
  }

  /** Look up the TPMS Status itemDescription_Enum for a given full status name.
   * Returns null when the name isn't a known TPMS Status variant. */
  getTpmsStatusEnum(statusName: string): number | null {
    return TPMS_STATUS_ENUM[statusName] ?? null;
  }
}
