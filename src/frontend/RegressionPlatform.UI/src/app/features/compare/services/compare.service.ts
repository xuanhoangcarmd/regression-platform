import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { catchError, Observable, of, switchMap, tap } from 'rxjs';

import { ProjectContextService } from '@core/services';

import { CompareApiService } from '../apis/compare-api.service';
import { CompareStatusFilter, ICompareCreate, ICompareJob } from '../models/compare.model';

/**
 * Owns the Compare History list state (search, status filter, paging). Backed by the API
 * (`/api/compares`), scoped to the active project. Filtering/paging is client-side.
 */
@Injectable({ providedIn: 'root' })
export class CompareService {
  private readonly api = inject(CompareApiService);
  private readonly projectContext = inject(ProjectContextService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly allCompares = signal<ICompareJob[]>([]);
  readonly loading = signal(false);

  readonly search = signal('');
  readonly status = signal<CompareStatusFilter>('all');
  readonly pageNumber = signal(1);
  readonly pageSize = signal(10);

  // Bumped to force a re-fetch (after a create, or while polling active jobs).
  private readonly refreshTrigger = signal(0);

  constructor() {
    // Reload when the project changes or after a refresh. switchMap cancels stale requests.
    toObservable(computed(() => ({ project: this.projectContext.code(), refresh: this.refreshTrigger() })))
      .pipe(
        switchMap((query) => {
          if (!query.project) {
            return of<ICompareJob[]>([]);
          }
          this.loading.set(true);
          return this.api.getCompares().pipe(catchError(() => of<ICompareJob[]>([])));
        }),
        tap(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((compares) => this.allCompares.set(compares));
  }

  /** True while at least one compare is actually executing (drives live polling). Pending waits for Start. */
  readonly hasActive = computed(() => this.allCompares().some((compare) => compare.status === 'Running'));

  private readonly filtered = computed(() => {
    const term = this.search().trim().toLowerCase();
    const status = this.status();
    return this.allCompares().filter((compare) => {
      const matchesStatus = status === 'all' || compare.status === status;
      const matchesTerm =
        !term ||
        compare.runATitle.toLowerCase().includes(term) ||
        compare.runBTitle.toLowerCase().includes(term);
      return matchesStatus && matchesTerm;
    });
  });

  readonly totalCount = computed(() => this.filtered().length);

  readonly compares = computed(() => {
    const start = (this.pageNumber() - 1) * this.pageSize();
    return this.filtered().slice(start, start + this.pageSize());
  });

  /**
   * Live poll while a compare is still executing. Driven by the History page on a timer while it's
   * open — so polling stops when you leave the list (no global background loop).
   *
   * SILENT refresh: refetches the list WITHOUT toggling `loading`, so a running compare's progress
   * updates in place instead of flashing the whole table to "Loading…" every tick. Mirrors the Runs
   * page poll (see RunService.pollVisibleProgress).
   */
  pollActive(): void {
    if (!this.hasActive()) {
      return;
    }
    this.api
      .getCompares()
      .pipe(
        catchError(() => of<ICompareJob[]>([])),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((compares) => this.allCompares.set(compares));
  }

  /** Creates a compare via the API, then refreshes the list; returns the call so callers can await it. */
  create(payload: ICompareCreate): Observable<ICompareJob> {
    return this.api.create(payload).pipe(
      tap(() => {
        this.pageNumber.set(1);
        this.refresh();
      }),
    );
  }

  /** Starts a pending compare, then refreshes; returns the call so callers can await it. */
  start(id: number): Observable<void> {
    return this.api.start(id).pipe(tap(() => this.refresh()));
  }

  /** Stops a running compare, then refreshes; returns the call so callers can await it. */
  stop(id: number): Observable<void> {
    return this.api.stop(id).pipe(tap(() => this.refresh()));
  }

  /** Deletes a compare (and all its data), then refreshes; returns the call so callers can await it. */
  remove(id: number): Observable<void> {
    return this.api.delete(id).pipe(tap(() => this.refresh()));
  }

  setSearch(term: string): void {
    this.search.set(term);
    this.pageNumber.set(1);
  }

  setStatus(status: CompareStatusFilter): void {
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

/** Formats the numeric id as the display id shown in the UI (e.g. CMP-018). */
export function displayCompareId(id: number): string {
  return `CMP-${String(id).padStart(3, '0')}`;
}
