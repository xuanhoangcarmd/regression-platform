import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { Observable, catchError, of, switchMap, tap } from 'rxjs';

import { ProjectContextService } from '@core/services';

import { DatasetApiService } from '../apis/dataset-api.service';
import { DatasetStatusFilter, IDataset, IDatasetCreate, IDatasetItem, IDatasetUpdate } from '../models/dataset.model';

/**
 * Owns the Dataset List state (search + status filter) for the active project, backed by
 * `/api/datasets`. The list is small, so filtering/paging is client-side.
 */
@Injectable({ providedIn: 'root' })
export class DatasetService {
  private readonly api = inject(DatasetApiService);
  private readonly projectContext = inject(ProjectContextService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly allDatasets = signal<IDatasetItem[]>([]);
  readonly loading = signal(false);

  readonly search = signal('');
  readonly status = signal<DatasetStatusFilter>('all');

  /** Pinned dataset ids for the active project (persisted per machine in localStorage). */
  private readonly pinnedIds = signal<ReadonlySet<number>>(new Set());

  private readonly refreshTrigger = signal(0);

  constructor() {
    toObservable(computed(() => ({ project: this.projectContext.code(), refresh: this.refreshTrigger() })))
      .pipe(
        switchMap((query) => {
          if (!query.project) {
            return of<IDatasetItem[]>([]);
          }
          this.loading.set(true);
          return this.api.getDatasets().pipe(catchError(() => of<IDatasetItem[]>([])));
        }),
        tap(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((datasets) => this.allDatasets.set(datasets));

    // Load this machine's pins whenever the active project changes (incl. the initial value on reload).
    // A subscription (not effect) so we can write the signal in this Angular version.
    toObservable(computed(() => this.projectContext.code()))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((code) => this.pinnedIds.set(this.loadPins(code)));
  }

  readonly datasets = computed(() => {
    const term = this.search().trim().toLowerCase();
    const status = this.status();
    const pinned = this.pinnedIds();
    return this.allDatasets()
      .filter((dataset) => {
        const matchesStatus =
          status === 'all' || (status === 'active' ? dataset.isActive : !dataset.isActive);
        const matchesTerm =
          !term ||
          dataset.name.toLowerCase().includes(term) ||
          (dataset.description ?? '').toLowerCase().includes(term);
        return matchesStatus && matchesTerm;
      })
      // Pinned datasets float to the top; within each group keep the backend's name order.
      .sort((a, b) => {
        const rank = (pinned.has(b.id) ? 1 : 0) - (pinned.has(a.id) ? 1 : 0);
        return rank !== 0 ? rank : a.name.localeCompare(b.name);
      });
  });

  isPinned(id: number): boolean {
    return this.pinnedIds().has(id);
  }

  /** Toggle a dataset's pin and persist it for this machine + project. */
  togglePin(id: number): void {
    const next = new Set(this.pinnedIds());
    if (next.has(id)) {
      next.delete(id);
    } else {
      next.add(id);
    }
    this.pinnedIds.set(next);
    this.savePins(this.projectContext.code(), next);
  }

  private pinStorageKey(projectCode: string): string {
    return `regression-platform:pinned-datasets:${projectCode}`;
  }

  private loadPins(projectCode: string): ReadonlySet<number> {
    if (!projectCode) {
      return new Set();
    }
    try {
      const raw = localStorage.getItem(this.pinStorageKey(projectCode));
      const ids = raw ? (JSON.parse(raw) as number[]) : [];
      return new Set(Array.isArray(ids) ? ids : []);
    } catch {
      return new Set();
    }
  }

  private savePins(projectCode: string, ids: ReadonlySet<number>): void {
    if (!projectCode) {
      return;
    }
    try {
      localStorage.setItem(this.pinStorageKey(projectCode), JSON.stringify([...ids]));
    } catch {
      // Ignore storage failures (private mode / quota) — pinning is a best-effort convenience.
    }
  }

  setSearch(term: string): void {
    this.search.set(term);
  }

  setStatus(status: DatasetStatusFilter): void {
    this.status.set(status);
  }

  getById(id: number): Observable<IDataset> {
    return this.api.getById(id);
  }

  /** Creates a dataset and refreshes the list; returns the call so callers can await completion. */
  create(payload: IDatasetCreate): Observable<number> {
    return this.api.create(payload).pipe(tap(() => this.refresh()));
  }

  /** Updates a dataset and refreshes the list; returns the call so callers can await completion. */
  update(id: number, payload: IDatasetUpdate): Observable<void> {
    return this.api.update(id, payload).pipe(tap(() => this.refresh()));
  }

  /** Deletes a dataset and refreshes the list; returns the call so callers can await completion. */
  remove(id: number): Observable<void> {
    return this.api.delete(id).pipe(tap(() => this.refresh()));
  }

  private refresh(): void {
    this.refreshTrigger.update((value) => value + 1);
  }
}
