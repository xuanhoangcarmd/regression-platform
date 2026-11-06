import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ProjectContextService } from '@core/services';

import { environment } from '../../../../environments/environment';
import {
  IReportDetail,
  IReportGenerate,
  IReportListItem,
  IReportPerformance,
  IReportPerformanceStats,
} from '../models/report.model';

/** HTTP access to the backend reports endpoints (`/api/reports`), scoped by the active project. */
@Injectable({ providedIn: 'root' })
export class ReportApiService {
  private readonly http = inject(HttpClient);
  private readonly projectContext = inject(ProjectContextService);
  private readonly baseUrl = `${environment.apiBaseUrl}/reports`;

  getReports(): Observable<IReportListItem[]> {
    return this.http.get<IReportListItem[]>(this.baseUrl, { params: this.projectParams() });
  }

  /** Generates (or regenerates, in place) the report for a Completed compare. */
  generate(payload: IReportGenerate): Observable<IReportListItem> {
    return this.http.post<IReportListItem>(this.baseUrl, payload, { params: this.projectParams() });
  }

  getById(id: number): Observable<IReportDetail> {
    return this.http.get<IReportDetail>(`${this.baseUrl}/${id}`, { params: this.projectParams() });
  }

  /** Deletes a report and its stored AI analysis (the compare/runs are untouched). */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`, { params: this.projectParams() });
  }

  /** Response-time comparison (Run A vs Run B) for the Distribution tab charts. */
  getPerformance(id: number): Observable<IReportPerformance> {
    return this.http.get<IReportPerformance>(`${this.baseUrl}/${id}/performance`, { params: this.projectParams() });
  }

  /** Latency histogram + per-API stats (p95/max/size) for the Distribution tab. */
  getPerformanceStats(id: number): Observable<IReportPerformanceStats> {
    return this.http.get<IReportPerformanceStats>(`${this.baseUrl}/${id}/performance-stats`, {
      params: this.projectParams(),
    });
  }

  private projectParams(): HttpParams {
    return new HttpParams().set('projectId', this.projectContext.code());
  }
}
