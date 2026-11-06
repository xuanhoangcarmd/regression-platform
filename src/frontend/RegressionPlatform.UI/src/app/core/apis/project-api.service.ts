import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { IProject } from '@core/models';

import { environment } from '../../../environments/environment';

/** HTTP access to the projects endpoint (`/api/projects`). */
@Injectable({ providedIn: 'root' })
export class ProjectApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/projects`;

  getProjects(): Observable<IProject[]> {
    return this.http.get<IProject[]>(this.baseUrl);
  }
}
