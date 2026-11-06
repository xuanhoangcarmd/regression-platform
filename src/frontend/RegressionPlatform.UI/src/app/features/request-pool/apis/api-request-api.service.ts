import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { IPagedResult } from '@core/models';

import { environment } from '../../../../environments/environment';
import {
  IApiRequestDetail,
  IApiRequestItem,
  IApiRequestWrite,
  IRequestImportResult,
} from '../models/api-request.model';

/** Query params for the paged request list within a dataset (matches the backend contract). */
export interface IApiRequestListQuery {
  datasetId: number;
  pageNumber: number;
  pageSize: number;
  apiId?: number;
  search?: string;
  isActive?: boolean;
  sortBy?: string | null;
  sortDirection?: 'asc' | 'desc';
}

/** HTTP access to the backend request endpoints (`/api/requests`), scoped by dataset. */
@Injectable({ providedIn: 'root' })
export class ApiRequestApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/requests`;

  getRequests(query: IApiRequestListQuery): Observable<IPagedResult<IApiRequestItem>> {
    let params = new HttpParams()
      .set('datasetId', query.datasetId)
      .set('pageNumber', query.pageNumber)
      .set('pageSize', query.pageSize);

    if (query.apiId !== undefined && query.apiId !== null) {
      params = params.set('apiId', query.apiId);
    }
    if (query.search) {
      params = params.set('search', query.search);
    }
    if (query.isActive !== undefined) {
      params = params.set('isActive', query.isActive);
    }
    if (query.sortBy) {
      params = params.set('sortBy', query.sortBy);
    }
    if (query.sortDirection) {
      params = params.set('sortDirection', query.sortDirection);
    }

    return this.http.get<IPagedResult<IApiRequestItem>>(this.baseUrl, { params });
  }

  getById(id: number): Observable<IApiRequestDetail> {
    return this.http.get<IApiRequestDetail>(`${this.baseUrl}/${id}`);
  }

  create(datasetId: number, payload: IApiRequestWrite): Observable<number> {
    return this.http.post<number>(this.baseUrl, { datasetId, ...payload });
  }

  update(id: number, payload: IApiRequestWrite): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, payload);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  /** Deletes ALL requests of an API within a dataset (+ their responses); returns the count removed. */
  deleteByApi(datasetId: number, apiId: number): Observable<number> {
    const params = new HttpParams().set('datasetId', datasetId).set('apiId', apiId);
    return this.http.delete<number>(`${this.baseUrl}/by-api`, { params });
  }

  /** Bulk-import requests for an API into a dataset from an Excel file (multipart upload). */
  importFromExcel(datasetId: number, apiId: number, file: File): Observable<IRequestImportResult> {
    const form = new FormData();
    form.append('datasetId', String(datasetId));
    form.append('apiId', String(apiId));
    form.append('file', file);
    return this.http.post<IRequestImportResult>(`${this.baseUrl}/import`, form);
  }
}
