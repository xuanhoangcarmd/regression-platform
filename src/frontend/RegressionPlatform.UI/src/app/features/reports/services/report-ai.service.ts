import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, of } from 'rxjs';

import { ProjectContextService } from '@core/services';

import { environment } from '../../../../environments/environment';
import { IReportAiAnalysis, IReportAiAskResponse } from '../models/report-ai.model';

/**
 * Fetches the AI analysis of a report from the backend (`/api/reports/{id}/ai-analysis`). The server
 * runs the configured AI provider (DeepSeek), falling back to a deterministic rule-based analysis
 * when no LLM is configured — the returned {@link IReportAiAnalysis.provider} tells which ran.
 */
@Injectable({ providedIn: 'root' })
export class ReportAiService {
  private readonly http = inject(HttpClient);
  private readonly projectContext = inject(ProjectContextService);
  private readonly baseUrl = `${environment.apiBaseUrl}/reports`;

  /** Load the stored analysis (generates + persists on first request). */
  getAnalysis(reportId: number): Observable<IReportAiAnalysis> {
    return this.http.get<IReportAiAnalysis>(`${this.baseUrl}/${reportId}/ai-analysis`, {
      params: new HttpParams().set('projectId', this.projectContext.code()),
    });
  }

  /** Force a fresh AI run for the report (the Regenerate button) and persist it in place. */
  regenerate(reportId: number): Observable<IReportAiAnalysis> {
    return this.http.post<IReportAiAnalysis>(`${this.baseUrl}/${reportId}/ai-analysis/regenerate`, null, {
      params: new HttpParams().set('projectId', this.projectContext.code()),
    });
  }

  /**
   * The already-stored analysis, or null if none has been generated yet. Never triggers a fresh AI
   * run — used by exports so they reuse existing data only.
   */
  getStoredAnalysis(reportId: number): Observable<IReportAiAnalysis | null> {
    return this.http
      .get<IReportAiAnalysis>(`${this.baseUrl}/${reportId}/ai-analysis/stored`, {
        params: new HttpParams().set('projectId', this.projectContext.code()),
      })
      .pipe(catchError(() => of(null)));
  }

  /**
   * Ask a single question about the report ("Ask AI about this report"). The server answers strictly
   * from the report's data (refusing off-topic questions); answers are cached ~15 min and not persisted.
   */
  ask(reportId: number, question: string): Observable<IReportAiAskResponse> {
    return this.http.post<IReportAiAskResponse>(
      `${this.baseUrl}/${reportId}/ai-analysis/ask`,
      { question },
      { params: new HttpParams().set('projectId', this.projectContext.code()) },
    );
  }
}
