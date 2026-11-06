import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { IPagedResult } from '@core/models';

import { environment } from '../../../../environments/environment';
import {
  IPayloadContent,
  IRawBufferContent,
  IVehicleProfileDetail,
  IVehicleProfileListItem,
  IVehicleProfileSearch,
  IYmmeOptions,
} from '../models/data-library.model';

/** HTTP access to the Data Library endpoints (`/api/data-library`) + the static YMME options file. */
@Injectable({ providedIn: 'root' })
export class DataLibraryApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/data-library`;

  /** Distinct YMME options from the data, faceted by the current selections (cascading filter). */
  getYmmeOptions(sel: { year?: string; make?: string; model?: string; engine?: string }): Observable<IYmmeOptions> {
    let params = new HttpParams();
    if (sel.year) params = params.set('year', sel.year);
    if (sel.make) params = params.set('make', sel.make);
    if (sel.model) params = params.set('model', sel.model);
    if (sel.engine) params = params.set('engine', sel.engine);
    return this.http.get<IYmmeOptions>(`${this.baseUrl}/ymme-options`, { params });
  }

  searchProfiles(query: IVehicleProfileSearch): Observable<IPagedResult<IVehicleProfileListItem>> {
    let params = new HttpParams().set('pageNumber', query.pageNumber).set('pageSize', query.pageSize);
    if (query.vin) params = params.set('vin', query.vin);
    if (query.year) params = params.set('year', query.year);
    if (query.make) params = params.set('make', query.make);
    if (query.model) params = params.set('model', query.model);
    if (query.engine) params = params.set('engine', query.engine);
    if (query.yearFrom !== undefined) params = params.set('yearFrom', query.yearFrom);
    if (query.yearTo !== undefined) params = params.set('yearTo', query.yearTo);
    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDirection) params = params.set('sortDirection', query.sortDirection);
    if (query.toolMilStatus !== undefined) params = params.set('toolMilStatus', query.toolMilStatus);
    if (query.toolLedStatus !== undefined) params = params.set('toolLedStatus', query.toolLedStatus);
    if (query.primaryDtc) params = params.set('primaryDtc', query.primaryDtc);
    if (query.primaryDtcMode) params = params.set('primaryDtcMode', query.primaryDtcMode);
    if (query.dtcCode) params = params.set('dtcCode', query.dtcCode);
    if (query.dtcCodeMode) params = params.set('dtcCodeMode', query.dtcCodeMode);
    if (query.dtcSource) params = params.set('dtcSource', query.dtcSource);
    if (query.dtcSystem) params = params.set('dtcSystem', query.dtcSystem);
    if (query.dtcStatusType) params = params.set('dtcStatusType', query.dtcStatusType);
    if (query.dtcDefinition) params = params.set('dtcDefinition', query.dtcDefinition);
    if (query.dtcFormat) params = params.set('dtcFormat', query.dtcFormat);
    if (query.dtcPrefix) params = params.set('dtcPrefix', query.dtcPrefix);
    if (query.hasRawBuffer !== undefined) params = params.set('hasRawBuffer', query.hasRawBuffer);
    return this.http.get<IPagedResult<IVehicleProfileListItem>>(`${this.baseUrl}/profiles`, { params });
  }

  getProfile(id: number): Observable<IVehicleProfileDetail> {
    return this.http.get<IVehicleProfileDetail>(`${this.baseUrl}/profiles/${id}`);
  }

  getPayload(id: number): Observable<IPayloadContent> {
    return this.http.get<IPayloadContent>(`${this.baseUrl}/payloads/${id}`);
  }

  getRawBuffer(id: number): Observable<IRawBufferContent> {
    return this.http.get<IRawBufferContent>(`${this.baseUrl}/raw-buffers/${id}`);
  }
}
