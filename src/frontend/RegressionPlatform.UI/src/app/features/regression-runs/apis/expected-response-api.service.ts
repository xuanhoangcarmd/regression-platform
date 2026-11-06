import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, concatMap, from, map, of, reduce, toArray } from 'rxjs';

import { ProjectContextService } from '@core/services';

import { environment } from '../../../../environments/environment';
import {
  IExpectedApi,
  IExpectedRequestItem,
  IExpectedResponseDetail,
  IExpectedSaveItem,
} from '../models/expected-response.model';

/** Server limit per details / save / restore call — larger selections are sent in sequential chunks. */
const MAX_BATCH = 1000;

function chunk<T>(items: T[], size: number): T[][] {
  const chunks: T[][] = [];
  for (let i = 0; i < items.length; i += size) {
    chunks.push(items.slice(i, i + size));
  }
  return chunks;
}

/**
 * HTTP access to a run's expected (edited) responses (`/api/runs/{runId}/expected`), scoped by the
 * active project. A compare in 'Expected' mode uses each run's edits.
 */
@Injectable({ providedIn: 'root' })
export class ExpectedResponseApiService {
  private readonly http = inject(HttpClient);
  private readonly projectContext = inject(ProjectContextService);

  /** APIs of the run with request + edited counts. */
  getApis(runId: number): Observable<IExpectedApi[]> {
    return this.http.get<IExpectedApi[]>(`${this.url(runId)}/apis`, { params: this.projectParams() });
  }

  /** The run's executed requests of one API (no bodies). */
  getRequests(runId: number, apiId: number): Observable<IExpectedRequestItem[]> {
    return this.http.get<IExpectedRequestItem[]>(`${this.url(runId)}/requests`, {
      params: this.projectParams().set('apiId', apiId),
    });
  }

  /** Original + edited bodies for any number of requests (chunked). */
  getDetails(runId: number, requestIds: number[]): Observable<IExpectedResponseDetail[]> {
    if (!requestIds.length) {
      return of([]);
    }
    return from(chunk(requestIds, MAX_BATCH)).pipe(
      concatMap((ids) =>
        this.http.post<IExpectedResponseDetail[]>(`${this.url(runId)}/details`, { requestIds: ids }, { params: this.projectParams() }),
      ),
      toArray(),
      map((pages) => pages.flat()),
    );
  }

  /** Saves (upserts) edited responses; returns how many were saved. Each chunk is one server transaction. */
  save(runId: number, items: IExpectedSaveItem[]): Observable<number> {
    return from(chunk(items, MAX_BATCH)).pipe(
      concatMap((part) => this.http.put<number>(this.url(runId), { items: part }, { params: this.projectParams() })),
      reduce((total, saved) => total + saved, 0),
    );
  }

  /** Deletes the edited versions → those requests compare against the original again. */
  restoreOriginal(runId: number, requestIds: number[]): Observable<number> {
    return from(chunk(requestIds, MAX_BATCH)).pipe(
      concatMap((ids) => this.http.post<number>(`${this.url(runId)}/restore`, { requestIds: ids }, { params: this.projectParams() })),
      reduce((total, removed) => total + removed, 0),
    );
  }

  /** How many of the run's responses have an edited version (compare-modal hint). */
  countEdited(runId: number): Observable<number> {
    return this.getApis(runId).pipe(map((apis) => apis.reduce((sum, api) => sum + api.editedCount, 0)));
  }

  private url(runId: number): string {
    return `${environment.apiBaseUrl}/runs/${runId}/expected`;
  }

  private projectParams(): HttpParams {
    return new HttpParams().set('projectId', this.projectContext.code());
  }
}
