import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { IEnvironmentConfigurationResponse, ISaveEnvironmentConfiguration } from '../models/environment-config.model';

/** HTTP access to per-project environment configuration (`/api/projects/{code}/configurations`). */
@Injectable({ providedIn: 'root' })
export class ProjectConfigurationApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/projects`;

  /** All environments' configuration for a project. */
  getConfigurations(projectCode: string): Observable<IEnvironmentConfigurationResponse[]> {
    return this.http.get<IEnvironmentConfigurationResponse[]>(`${this.baseUrl}/${projectCode}/configurations`);
  }

  /** Save one environment's configuration (upsert + replace headers). */
  save(projectCode: string, environmentKey: string, payload: ISaveEnvironmentConfiguration): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${projectCode}/configurations/${environmentKey}`, payload);
  }
}
