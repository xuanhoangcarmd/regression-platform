import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { IPagedResult } from '@core/models';
import { ProjectContextService } from '@core/services';

import { environment } from '../../../../environments/environment';
import {
  IRun,
  IRunCreate,
  IRunDetail,
  IRunEdit,
  IRunProgress,
  IRunResponseDetail,
  IRunResponseExportRow,
  IRunResponseItem,
} from '../models/run.model';

/** Query for a run's executed-request list. */
export interface IRunResponseQuery {
  pageNumber: number;
  pageSize: number;
  search?: string;
  status?: string;
  apiId?: number | null;
  sortBy?: string | null;
  sortDirection?: 'asc' | 'desc';
}

/** HTTP access to the backend runs endpoints (`/api/runs`), scoped by the active project. */
@Injectable({ providedIn: 'root' })
export class RunApiService {
  private readonly http = inject(HttpClient);
  private readonly projectContext = inject(ProjectContextService);
  private readonly baseUrl = `${environment.apiBaseUrl}/runs`;

  getRuns(): Observable<IRun[]> {
    return this.http.get<IRun[]>(this.baseUrl, { params: this.projectParams() });
  }

  getById(id: number): Observable<IRunDetail> {
    return this.http.get<IRunDetail>(`${this.baseUrl}/${id}`, { params: this.projectParams() });
  }

  /**
   * Live progress for specific runs (poll target). Pass only the ids of runs currently visible and
   * executing so the request stays cheap regardless of how many runs exist.
   */
  getProgress(ids: number[]): Observable<IRunProgress[]> {
    let params = this.projectParams();
    for (const id of ids) {
      params = params.append('ids', id);
    }
    return this.http.get<IRunProgress[]>(`${this.baseUrl}/progress`, { params });
  }

  /** Paged list of a run's executed requests (Requests tab). */
  /** Every executed request of a run (API, input, full response, timing) — for the Excel export. */
  getExport(id: number): Observable<IRunResponseExportRow[]> {
    return this.http.get<IRunResponseExportRow[]>(`${this.baseUrl}/${id}/export`, { params: this.projectParams() });
  }

  getResponses(id: number, query: IRunResponseQuery): Observable<IPagedResult<IRunResponseItem>> {
    let params = this.projectParams().set('pageNumber', query.pageNumber).set('pageSize', query.pageSize);
    if (query.search) {
      params = params.set('search', query.search);
    }
    if (query.status && query.status !== 'all') {
      params = params.set('status', query.status);
    }
    if (query.apiId != null) {
      params = params.set('apiId', query.apiId);
    }
    if (query.sortBy) {
      params = params.set('sortBy', query.sortBy);
    }
    if (query.sortDirection) {
      params = params.set('sortDirection', query.sortDirection);
    }
    return this.http.get<IPagedResult<IRunResponseItem>>(`${this.baseUrl}/${id}/responses`, { params });
  }

  /** Full detail of one executed request in a run (Results tab). */
  getResponseDetail(id: number, requestId: number): Observable<IRunResponseDetail> {
    return this.http.get<IRunResponseDetail>(`${this.baseUrl}/${id}/responses/${requestId}`, {
      params: this.projectParams(),
    });
  }

  create(payload: IRunCreate): Observable<IRun> {
    return this.http.post<IRun>(this.baseUrl, payload, { params: this.projectParams() });
  }

  /** Editable fields of a pending run (to prefill the edit form). */
  getForEdit(id: number): Observable<IRunEdit> {
    return this.http.get<IRunEdit>(`${this.baseUrl}/${id}/edit`, { params: this.projectParams() });
  }

  /** Updates a pending run in place. */
  update(id: number, payload: IRunCreate): Observable<IRun> {
    return this.http.put<IRun>(`${this.baseUrl}/${id}`, payload, { params: this.projectParams() });
  }

  /** Starts a pending run (queues it for background execution). */
  start(id: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/start`, null, { params: this.projectParams() });
  }

  /** Stops a running run (marks it Cancelled and aborts execution). */
  stop(id: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/stop`, null, { params: this.projectParams() });
  }

  /** Re-runs a single request inside a run and updates its stored result. */
  rerunRequest(id: number, requestId: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/responses/${requestId}/rerun`, null, { params: this.projectParams() });
  }

  /** Deletes a run and ALL data derived from it (responses, compares, reports). */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`, { params: this.projectParams() });
  }

  private projectParams(): HttpParams {
    return new HttpParams().set('projectId', this.projectContext.code());
  }
}
