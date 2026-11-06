import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { IPagedResult } from '@core/models';
import { ProjectContextService } from '@core/services';

import { environment } from '../../../../environments/environment';
import { IApiDefinition, IApiImportResult } from '../models/api-definition.model';

/** Query params for the paged API list (matches the backend contract). */
export interface IApiListQuery {
  pageNumber: number;
  pageSize: number;
  search?: string;
  isActive?: boolean;
  sortBy?: string | null;
  sortDirection?: 'asc' | 'desc';
}

/** Write payload for create/update. */
export interface IApiWritePayload {
  name: string;
  method: string;
  endpoint: string;
  description?: string | null;
  isActive: boolean;
}

/** HTTP access to the backend API-definitions endpoints (`/api/apis`). */
@Injectable({ providedIn: 'root' })
export class ApiDefinitionApiService {
  private readonly http = inject(HttpClient);
  private readonly projectContext = inject(ProjectContextService);
  private readonly baseUrl = `${environment.apiBaseUrl}/apis`;

  /** The current project code, sent as `?projectId=` so the backend scopes the request. */
  private get projectId(): string {
    return this.projectContext.code();
  }

  getApis(query: IApiListQuery): Observable<IPagedResult<IApiDefinition>> {
    let params = new HttpParams()
      .set('projectId', this.projectId)
      .set('pageNumber', query.pageNumber)
      .set('pageSize', query.pageSize);

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

    return this.http.get<IPagedResult<IApiDefinition>>(this.baseUrl, { params });
  }

  create(payload: IApiWritePayload): Observable<IApiDefinition> {
    return this.http.post<IApiDefinition>(this.baseUrl, payload, { params: this.projectParams() });
  }

  update(id: number, payload: IApiWritePayload): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, payload, { params: this.projectParams() });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  importFromSwagger(swaggerUrl: string): Observable<IApiImportResult> {
    return this.http.post<IApiImportResult>(`${this.baseUrl}/import`, { swaggerUrl }, { params: this.projectParams() });
  }

  private projectParams(): HttpParams {
    return new HttpParams().set('projectId', this.projectId);
  }
}
