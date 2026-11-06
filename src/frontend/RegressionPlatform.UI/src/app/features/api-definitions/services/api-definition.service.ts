import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { catchError, Observable, of, switchMap, tap } from 'rxjs';

import { IPagedResult, PaginationDefaults, SortDirection } from '@core/models';
import { ProjectContextService } from '@core/services';

import { ApiDefinitionApiService } from '../apis/api-definition-api.service';
import { ApiDefinitionCreate, ApiStatusFilter, IApiDefinition, IApiImportResult } from '../models/api-definition.model';

export type ApiDefinitionSortField = 'id' | 'name' | 'method' | 'endpoint' | 'requestCount' | 'isActive';

/**
 * Owns the API-list query state (search, status, sort, paging) and reads/writes
 * the backend (/api/apis). Query changes re-fetch automatically; create/update/delete
 * refresh the current page.
 */
@Injectable({ providedIn: 'root' })
export class ApiDefinitionService {
  private readonly api = inject(ApiDefinitionApiService);
  private readonly projectContext = inject(ProjectContextService);
  private readonly destroyRef = inject(DestroyRef);

  readonly search = signal('');
  readonly status = signal<ApiStatusFilter>('all');
  readonly sortBy = signal<ApiDefinitionSortField | null>(null);
  readonly sortDirection = signal<SortDirection>('asc');
  readonly pageNumber = signal<number>(PaginationDefaults.pageNumber);
  readonly pageSize = signal<number>(10);

  private static readonly emptyPage: IPagedResult<IApiDefinition> = {
    items: [],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0,
  };

  private readonly resultState = signal<IPagedResult<IApiDefinition>>(ApiDefinitionService.emptyPage);

  /** The current page of results from the backend. */
  readonly result = this.resultState.asReadonly();

  private readonly loadingState = signal(false);
  /** True while a list page is being fetched (search / status / sort / paging / refresh). */
  readonly loading = this.loadingState.asReadonly();

  // Bumped to force a re-fetch after a create/update/delete.
  private readonly refreshTrigger = signal(0);

  private readonly query = computed(() => ({
    // Re-fetch when the active project changes (the API service scopes by project).
    project: this.projectContext.code(),
    search: this.search(),
    status: this.status(),
    sortBy: this.sortBy(),
    sortDirection: this.sortDirection(),
    pageNumber: this.pageNumber(),
    pageSize: this.pageSize(),
    refresh: this.refreshTrigger(),
  }));

  constructor() {
    toObservable(this.query)
      .pipe(
        tap(() => this.loadingState.set(true)),
        switchMap((query) =>
          // No active project yet (briefly, before the first navigation resolves) → empty page.
          !query.project
            ? of(ApiDefinitionService.emptyPage)
            : this.api
                .getApis({
                  pageNumber: query.pageNumber,
                  pageSize: query.pageSize,
                  search: query.search.trim() || undefined,
                  isActive: query.status === 'all' ? undefined : query.status === 'active',
                  sortBy: query.sortBy,
                  sortDirection: query.sortDirection,
                })
                .pipe(catchError(() => of(ApiDefinitionService.emptyPage))),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((page) => {
        this.resultState.set(page);
        this.loadingState.set(false);
      });
  }

  setSearch(term: string): void {
    this.search.set(term);
    this.pageNumber.set(1);
  }

  setStatus(status: ApiStatusFilter): void {
    this.status.set(status);
    this.pageNumber.set(1);
  }

  /** Toggle direction when re-sorting the same field, otherwise sort ascending. */
  toggleSort(field: ApiDefinitionSortField): void {
    if (this.sortBy() === field) {
      this.sortDirection.update((dir) => (dir === 'asc' ? 'desc' : 'asc'));
    } else {
      this.sortBy.set(field);
      this.sortDirection.set('asc');
    }
    this.pageNumber.set(1);
  }

  setPage(pageNumber: number): void {
    this.pageNumber.set(pageNumber);
  }

  /** Creates an API and refreshes the list; returns the create call so callers can await completion. */
  add(data: ApiDefinitionCreate): Observable<IApiDefinition> {
    return this.api.create({ ...data, description: null }).pipe(
      tap(() => {
        this.pageNumber.set(1);
        this.refresh();
      }),
    );
  }

  /** Updates an API and refreshes the list; returns the update call so callers can await completion. */
  update(updated: IApiDefinition): Observable<void> {
    return this.api
      .update(updated.id, {
        name: updated.name,
        method: updated.method,
        endpoint: updated.endpoint,
        description: updated.description ?? null,
        isActive: updated.isActive,
      })
      .pipe(tap(() => this.refresh()));
  }

  /** Deletes an API and refreshes the list; returns the delete call so callers can await completion. */
  remove(id: number): Observable<void> {
    return this.api.delete(id).pipe(tap(() => this.refresh()));
  }

  /** Imports APIs from a Swagger URL and refreshes the list; returns the import result. */
  importFromSwagger(swaggerUrl: string): Observable<IApiImportResult> {
    return this.api.importFromSwagger(swaggerUrl).pipe(
      tap(() => {
        this.pageNumber.set(1);
        this.refresh();
      }),
    );
  }

  private refresh(): void {
    this.refreshTrigger.update((value) => value + 1);
  }
}

/** Formats the numeric PK as the display id shown in the table (e.g. API-001). */
export function displayApiId(id: number): string {
  return `API-${String(id).padStart(3, '0')}`;
}
