import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { catchError, EMPTY, map, Observable, of, switchMap, tap } from 'rxjs';

import { IPagedResult, PaginationDefaults, SortDirection } from '@core/models';
import { ProjectContextService } from '@core/services';
import { ISelectOption } from '@shared/components';
// Reused cross-feature for the API filter dropdown (candidate to promote to core/apis).
import { ApiDefinitionApiService } from '@features/api-definitions/apis/api-definition-api.service';

import { ApiRequestApiService } from '../apis/api-request-api.service';
import {
  IApiRequestDetail,
  IApiRequestItem,
  IApiRequestWrite,
  IRequestImportResult,
  RequestStatusFilter,
} from '../models/api-request.model';

export type ApiRequestSortField = 'id' | 'apiName' | 'title' | 'description' | 'method' | 'isActive';

/**
 * Owns the request-list query state (API filter, title search, status, paging) for ONE dataset
 * and reads the backend (/api/requests?datasetId=). The owning page sets the active dataset via
 * {@link setDataset}. Also loads the project's API options for the filter/create form.
 */
@Injectable({ providedIn: 'root' })
export class ApiRequestService {
  private readonly api = inject(ApiRequestApiService);
  private readonly apiDefinitionApi = inject(ApiDefinitionApiService);
  private readonly projectContext = inject(ProjectContextService);
  private readonly destroyRef = inject(DestroyRef);

  /** The dataset whose requests are listed (set by the page from the route). */
  readonly datasetId = signal<number | null>(null);
  readonly apiId = signal<number | null>(null);
  readonly search = signal('');
  readonly status = signal<RequestStatusFilter>('all');
  readonly sortBy = signal<ApiRequestSortField | null>(null);
  readonly sortDirection = signal<SortDirection>('asc');
  readonly pageNumber = signal<number>(PaginationDefaults.pageNumber);
  readonly pageSize = signal<number>(10);

  /** API options (id → name) for the filter dropdown and the create/edit form. */
  readonly apiOptions = signal<ISelectOption[]>([]);

  private static readonly emptyPage: IPagedResult<IApiRequestItem> = {
    items: [],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0,
  };

  private readonly resultState = signal<IPagedResult<IApiRequestItem>>(ApiRequestService.emptyPage);
  readonly result = this.resultState.asReadonly();

  private readonly loadingState = signal(false);
  /** True while a list page is being fetched (API filter / search / status / paging / refresh). */
  readonly loading = this.loadingState.asReadonly();

  private readonly refreshTrigger = signal(0);

  private readonly query = computed(() => ({
    datasetId: this.datasetId(),
    apiId: this.apiId(),
    search: this.search(),
    status: this.status(),
    sortBy: this.sortBy(),
    sortDirection: this.sortDirection(),
    pageNumber: this.pageNumber(),
    pageSize: this.pageSize(),
    refresh: this.refreshTrigger(),
  }));

  constructor() {
    // API options for the filter/form — reload reactively when the active project changes.
    // switchMap cancels any in-flight request so a stale response can't overwrite a newer one.
    toObservable(computed(() => this.projectContext.code()))
      .pipe(
        switchMap((projectCode) =>
          projectCode
            ? this.apiDefinitionApi.getApis({ pageNumber: 1, pageSize: 100, isActive: true }).pipe(
                map((page) => page.items.map((api) => ({ value: String(api.id), label: api.name }))),
                catchError(() => of<ISelectOption[]>([])),
              )
            : of<ISelectOption[]>([]),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((options) => this.apiOptions.set(options));

    toObservable(this.query)
      .pipe(
        tap(() => this.loadingState.set(true)),
        switchMap((query) =>
          // No active dataset yet (briefly, before the page sets it) → empty page.
          !query.datasetId
            ? of(ApiRequestService.emptyPage)
            : this.api
                .getRequests({
                  datasetId: query.datasetId,
                  pageNumber: query.pageNumber,
                  pageSize: query.pageSize,
                  apiId: query.apiId ?? undefined,
                  search: query.search.trim() || undefined,
                  isActive: query.status === 'all' ? undefined : query.status === 'active',
                  sortBy: query.sortBy,
                  sortDirection: query.sortDirection,
                })
                .pipe(catchError(() => of(ApiRequestService.emptyPage))),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((page) => {
        this.resultState.set(page);
        this.loadingState.set(false);
      });
  }

  /** Point the list at a dataset (resets filters + paging when it changes). */
  setDataset(datasetId: number): void {
    if (this.datasetId() === datasetId) {
      return;
    }
    this.datasetId.set(datasetId);
    this.apiId.set(null);
    this.search.set('');
    this.status.set('all');
    this.pageNumber.set(1);
  }

  setApiId(apiId: number | null): void {
    this.apiId.set(apiId);
    this.pageNumber.set(1);
  }

  setSearch(term: string): void {
    this.search.set(term);
    this.pageNumber.set(1);
  }

  setStatus(status: RequestStatusFilter): void {
    this.status.set(status);
    this.pageNumber.set(1);
  }

  setPage(pageNumber: number): void {
    this.pageNumber.set(pageNumber);
  }

  getById(id: number): Observable<IApiRequestDetail> {
    return this.api.getById(id);
  }

  /** Creates a request in the active dataset and refreshes the list; returns the call to await. */
  create(payload: IApiRequestWrite): Observable<number> {
    const datasetId = this.datasetId();
    if (datasetId === null) {
      return EMPTY;
    }
    return this.api.create(datasetId, payload).pipe(
      tap(() => {
        this.pageNumber.set(1);
        this.refresh();
      }),
    );
  }

  /** Updates a request and refreshes the list; returns the call to await. */
  update(id: number, payload: IApiRequestWrite): Observable<void> {
    return this.api.update(id, payload).pipe(tap(() => this.refresh()));
  }

  /** Bulk-import from Excel into the active dataset; refreshes the list (back to page 1) on success. */
  importFromExcel(apiId: number, file: File): Observable<IRequestImportResult> {
    const datasetId = this.datasetId() ?? 0;
    return this.api.importFromExcel(datasetId, apiId, file).pipe(
      tap(() => {
        this.pageNumber.set(1);
        this.refresh();
      }),
    );
  }

  /** Deletes a request and refreshes the list; returns the call to await. */
  remove(id: number): Observable<void> {
    return this.api.delete(id).pipe(tap(() => this.refresh()));
  }

  /** Deletes ALL requests (+ responses) of an API in the active dataset; refreshes the list. */
  removeByApi(apiId: number): Observable<number> {
    const datasetId = this.datasetId();
    if (datasetId === null) {
      return EMPTY;
    }
    return this.api.deleteByApi(datasetId, apiId).pipe(
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

/** Formats the numeric PK as the display id shown in the table (e.g. REQ-00001). */
export function displayRequestId(id: number): string {
  return `REQ-${String(id).padStart(5, '0')}`;
}
