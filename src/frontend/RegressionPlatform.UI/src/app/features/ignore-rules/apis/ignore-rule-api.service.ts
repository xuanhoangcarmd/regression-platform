import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ProjectContextService } from '@core/services';

import { environment } from '../../../../environments/environment';
import { IIgnoreRule, IIgnoreRuleWrite } from '../models/ignore-rule.model';

/** HTTP access to the backend ignore-rule endpoints (`/api/ignore-rules`). */
@Injectable({ providedIn: 'root' })
export class IgnoreRuleApiService {
  private readonly http = inject(HttpClient);
  private readonly projectContext = inject(ProjectContextService);
  private readonly baseUrl = `${environment.apiBaseUrl}/ignore-rules`;

  getRules(): Observable<IIgnoreRule[]> {
    return this.http.get<IIgnoreRule[]>(this.baseUrl, { params: this.projectParams() });
  }

  create(payload: IIgnoreRuleWrite): Observable<IIgnoreRule> {
    return this.http.post<IIgnoreRule>(this.baseUrl, payload, { params: this.projectParams() });
  }

  update(id: number, payload: IIgnoreRuleWrite): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, payload);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  private projectParams(): HttpParams {
    return new HttpParams().set('projectId', this.projectContext.code());
  }
}
