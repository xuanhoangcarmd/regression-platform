import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { catchError, map, of, switchMap, tap } from 'rxjs';

import { ProjectContextService } from '@core/services';
import { DatasetApiService } from '@features/datasets/apis/dataset-api.service';
import { ISelectOption } from '@shared/components';

import { Observable } from 'rxjs';

import { RunApiService } from '../apis/run-api.service';
import { IRun, IRunCreate, IRunEdit, IRunProgress, RunStatusFilter } from '../models/run.model';

/**
 * Owns the Runs list state (search, status filter, paging) + the API options for the create form.
 * Backed by the API (`/api/runs`), scoped to the active project. Filtering/paging is client-side.
 */
@Injectable({ providedIn: 'root' })
export class RunService {
  private readonly api = inject(RunApiService);
  private readonly datasetApi = inject(DatasetApiService);
  private readonly projectContext = inject(ProjectContextService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly allRuns = signal<IRun[]>([]);
  readonly loading = signal(false);

  readonly search = signal('');
  readonly status = signal<RunStatusFilter>('all');
  readonly pageNumber = signal(1);
  readonly pageSize = signal(10);

  /** Active-dataset options (id → name) for the create-run form, scoped to the active project. */
  readonly datasetOptions = signal<ISelectOption[]>([]);

  // Bumped to force a re-fetch after a create.
  private readonly refreshTrigger = signal(0);

  constructor() {
    // Runs — reload when the project changes or after a create. switchMap cancels stale requests.
    toObservable(computed(() => ({ project: this.projectContext.code(), refresh: this.refreshTrigger() })))
      .pipe(
        switchMap((query) => {
          if (!query.project) {
            return of<IRun[]>([]);
          }
          this.loading.set(true);
          return this.api.getRuns().pipe(catchError(() => of<IRun[]>([])));
        }),
        tap(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((runs) => this.allRuns.set(runs));

    // Dataset options for the create form — only Active datasets, reload when the project changes.
    toObservable(computed(() => this.projectContext.code()))
      .pipe(
        switchMap((projectCode) =>
          projectCode
            ? this.datasetApi.getDatasets().pipe(
                map((datasets) =>
                  datasets
                    .filter((dataset) => dataset.isActive)
                    .map((dataset) => ({ value: String(dataset.id), label: dataset.name })),
                ),
                catchError(() => of<ISelectOption[]>([])),
              )
            : of<ISelectOption[]>([]),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((options) => this.datasetOptions.set(options));
  }

  /**
   * Poll live progress for the runs currently visible on the page that are still executing, then
   * merge the result back into the list. Sends only those run ids (not the whole list), and no-ops
   * when nothing on screen is running — so it stays cheap and doesn't refetch the full list.
   * Driven by the Runs list page on a timer; scoped to that page so it stops on navigation away.
   */
  pollVisibleProgress(): void {
    // Queued runs are polled too, so the row flips to Running (with progress) as soon as the
    // background worker picks them up.
    const runningIds = this.runs()
      .filter((run) => run.status === 'Running' || run.status === 'Queued')
      .map((run) => run.id);
    if (runningIds.length === 0) {
      return;
    }

    this.api
      .getProgress(runningIds)
      .pipe(
        catchError(() => of<IRunProgress[]>([])),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((progress) => this.mergeProgress(progress));
  }

  /** Overlay polled progress onto the matching runs (by id); other fields/rows are untouched. */
  private mergeProgress(progress: IRunProgress[]): void {
    if (progress.length === 0) {
      return;
    }
    const byId = new Map(progress.map((item) => [item.id, item]));
    this.allRuns.update((runs) => runs.map((run) => ({ ...run, ...byId.get(run.id) })));
  }

  private readonly filtered = computed(() => {
    const term = this.search().trim().toLowerCase();
    const status = this.status();
    return this.allRuns().filter((run) => {
      const matchesStatus = status === 'all' || run.status === status;
      const matchesTerm =
        !term || run.title.toLowerCase().includes(term) || (run.description ?? '').toLowerCase().includes(term);
      return matchesStatus && matchesTerm;
    });
  });

  readonly totalCount = computed(() => this.filtered().length);

  readonly runs = computed(() => {
    const start = (this.pageNumber() - 1) * this.pageSize();
    return this.filtered().slice(start, start + this.pageSize());
  });

  /** Creates a run via the API, then refreshes the list. */
  create(payload: IRunCreate): void {
    this.api
      .create(payload)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.pageNumber.set(1);
        this.refresh();
      });
  }

  /** Loads a pending run's editable fields (to prefill the edit modal). */
  getForEdit(id: number): Observable<IRunEdit> {
    return this.api.getForEdit(id);
  }

  /** Updates a pending run via the API, then refreshes the list. */
  update(id: number, payload: IRunCreate): void {
    this.api
      .update(id, payload)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.refresh());
  }

  /** Starts a pending run, then refreshes (polling then tracks it to completion). */
  start(id: number): void {
    this.api
      .start(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.refresh());
  }

  /** Stops a running run, then refreshes so its status flips to Cancelled. */
  stop(id: number): void {
    this.api
      .stop(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.refresh());
  }

  /**
   * Deletes a run (and its data), refreshing the list on success. Returns the observable so the
   * caller can keep its Delete button locked/loading until the request completes.
   */
  remove(id: number): Observable<void> {
    return this.api.delete(id).pipe(tap(() => this.refresh()));
  }

  setSearch(term: string): void {
    this.search.set(term);
    this.pageNumber.set(1);
  }

  setStatus(status: RunStatusFilter): void {
    this.status.set(status);
    this.pageNumber.set(1);
  }

  setPage(pageNumber: number): void {
    this.pageNumber.set(pageNumber);
  }

  private refresh(): void {
    this.refreshTrigger.update((value) => value + 1);
  }
}

/** Formats the numeric id as the display id shown in the table (e.g. RUN-8921). */
export function displayRunId(id: number): string {
  return `RUN-${id}`;
}

/**
 * Display label for a request/API result status. The internal value stays 'Passed' (used for tones
 * and the backend status filter), but it reads better as 'Success' for an API response — so only the
 * shown text is mapped. Other statuses are shown as-is.
 */
export function resultStatusLabel(status: string): string {
  return status === 'Passed' ? 'Success' : status;
}

/** Formats a duration in ms as "Xm Ys" / "Ys" (null → em dash). */
export function formatDuration(ms: number | null): string {
  if (ms === null) {
    return '—';
  }
  const totalSeconds = Math.round(ms / 1000);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return minutes > 0 ? `${minutes}m ${seconds}s` : `${seconds}s`;
}
