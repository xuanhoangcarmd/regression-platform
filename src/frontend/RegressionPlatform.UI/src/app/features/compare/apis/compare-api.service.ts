import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { IPagedResult } from '@core/models';
import { ProjectContextService } from '@core/services';

import { environment } from '../../../../environments/environment';
import {
  ICompareApiStatistic,
  ICompareChangeSummary,
  ICompareCreate,
  ICompareDetailDiff,
  ICompareDetailItem,
  ICompareFullDetail,
  ICompareJob,
  ICompareSummary,
  ICompareTestResultRow,
} from '../models/compare.model';

/** Query for a compare's per-request detail list. */
export interface ICompareDetailQuery {
  pageNumber: number;
  pageSize: number;
  search?: string;
  result?: string;
  apiId?: number | null;
  sortBy?: string | null;
  sortDirection?: 'asc' | 'desc';
  /** Filter to requests whose field-diff contains this change pattern (from AI Insights). */
  changeType?: string | null;
  /** Index-normalized path of the change pattern (e.g. $.data.Monitors[*].status). */
  changePath?: string | null;
}

/** HTTP access to the backend compares endpoints (`/api/compares`), scoped by the active project. */
@Injectable({ providedIn: 'root' })
export class CompareApiService {
  private readonly http = inject(HttpClient);
  private readonly projectContext = inject(ProjectContextService);
  private readonly baseUrl = `${environment.apiBaseUrl}/compares`;

  getCompares(): Observable<ICompareJob[]> {
    return this.http.get<ICompareJob[]>(this.baseUrl, { params: this.projectParams() });
  }

  create(payload: ICompareCreate): Observable<ICompareJob> {
    return this.http.post<ICompareJob>(this.baseUrl, payload, { params: this.projectParams() });
  }

  /** Starts a pending compare (queues it for background execution). */
  start(id: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/start`, null, { params: this.projectParams() });
  }

  /** Stops a running compare (marks it Cancelled and aborts the worker). */
  stop(id: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/stop`, null, { params: this.projectParams() });
  }

  /** Deletes a compare and all its data (blocked server-side while Running or if a report exists). */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`, { params: this.projectParams() });
  }

  getById(id: number): Observable<ICompareSummary> {
    return this.http.get<ICompareSummary>(`${this.baseUrl}/${id}`, { params: this.projectParams() });
  }

  getApiStatistics(id: number): Observable<ICompareApiStatistic[]> {
    return this.http.get<ICompareApiStatistic[]>(`${this.baseUrl}/${id}/api-statistics`, {
      params: this.projectParams(),
    });
  }

  /** Distinct change patterns of a compare (compact summary for review / AI). */
  getChangeSummary(id: number): Observable<ICompareChangeSummary> {
    return this.http.get<ICompareChangeSummary>(`${this.baseUrl}/${id}/change-summary`, {
      params: this.projectParams(),
    });
  }

  /** Paged per-request detail list (search + result filter + sort). */
  getDetails(id: number, query: ICompareDetailQuery): Observable<IPagedResult<ICompareDetailItem>> {
    let params = this.projectParams().set('pageNumber', query.pageNumber).set('pageSize', query.pageSize);
    if (query.search) {
      params = params.set('search', query.search);
    }
    if (query.result && query.result !== 'all') {
      params = params.set('result', query.result);
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
    if (query.changeType) {
      params = params.set('changeType', query.changeType);
    }
    if (query.changePath) {
      params = params.set('changePath', query.changePath);
    }
    return this.http.get<IPagedResult<ICompareDetailItem>>(`${this.baseUrl}/${id}/details`, { params });
  }

  /** All per-request details (identity + result + raw field diff) — for the full Excel export. */
  getFullDetails(id: number): Observable<ICompareFullDetail[]> {
    return this.http.get<ICompareFullDetail[]>(`${this.baseUrl}/${id}/details/full`, { params: this.projectParams() });
  }

  /** Every request with its input, both runs' responses and the outcome — for the Test Results export. */
  getTestResults(id: number): Observable<ICompareTestResultRow[]> {
    return this.http.get<ICompareTestResultRow[]>(`${this.baseUrl}/${id}/details/test-results`, {
      params: this.projectParams(),
    });
  }

  /** Full diff for one request (field diff + both runs' bodies). */
  getDetailDiff(id: number, requestId: number): Observable<ICompareDetailDiff> {
    return this.http.get<ICompareDetailDiff>(`${this.baseUrl}/${id}/details/${requestId}`, {
      params: this.projectParams(),
    });
  }

  private projectParams(): HttpParams {
    return new HttpParams().set('projectId', this.projectContext.code());
  }
}
