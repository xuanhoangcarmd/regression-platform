import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ProjectContextService } from '@core/services';

import { environment } from '../../../../environments/environment';
import { IDataset, IDatasetApi, IDatasetCreate, IDatasetItem, IDatasetUpdate } from '../models/dataset.model';

/** HTTP access to the backend dataset endpoints (`/api/datasets`), scoped by the active project. */
@Injectable({ providedIn: 'root' })
export class DatasetApiService {
  private readonly http = inject(HttpClient);
  private readonly projectContext = inject(ProjectContextService);
  private readonly baseUrl = `${environment.apiBaseUrl}/datasets`;

  getDatasets(): Observable<IDatasetItem[]> {
    const params = new HttpParams().set('projectId', this.projectContext.code());
    return this.http.get<IDatasetItem[]>(this.baseUrl, { params });
  }

  getById(id: number): Observable<IDataset> {
    return this.http.get<IDataset>(`${this.baseUrl}/${id}`);
  }

  /** APIs that have requests in the dataset (for the run-create API picker). */
  getApis(id: number): Observable<IDatasetApi[]> {
    return this.http.get<IDatasetApi[]>(`${this.baseUrl}/${id}/apis`);
  }

  create(payload: IDatasetCreate): Observable<number> {
    const params = new HttpParams().set('projectId', this.projectContext.code());
    return this.http.post<number>(this.baseUrl, payload, { params });
  }

  update(id: number, payload: IDatasetUpdate): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, payload);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
