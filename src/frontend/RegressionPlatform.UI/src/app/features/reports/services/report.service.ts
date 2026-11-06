import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { Observable, catchError, of, switchMap, tap } from 'rxjs';

import { ProjectContextService } from '@core/services';

import { ReportApiService } from '../apis/report-api.service';
import { IReportGenerate, IReportListItem, ReportStatusFilter } from '../models/report.model';

/**
 * Owns the Reports list state (search, status filter, paging). Backed by the API
 * (`/api/reports`), scoped to the active project. Filtering/paging is client-side.
 */
@Injectable({ providedIn: 'root' })
export class ReportService {
  private readonly api = inject(ReportApiService);
  private readonly projectContext = inject(ProjectContextService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly allReports = signal<IReportListItem[]>([]);
  readonly loading = signal(false);

  readonly search = signal('');
  readonly status = signal<ReportStatusFilter>('all');
  readonly pageNumber = signal(1);
  readonly pageSize = signal(10);

  // Bumped to force a re-fetch (after generating a report).
  private readonly refreshTrigger = signal(0);

  constructor() {
    // Reload when the project changes or after a refresh. switchMap cancels stale requests.
    toObservable(computed(() => ({ project: this.projectContext.code(), refresh: this.refreshTrigger() })))
      .pipe(
        switchMap((query) => {
          if (!query.project) {
            return of<IReportListItem[]>([]);
          }
          this.loading.set(true);
          return this.api.getReports().pipe(catchError(() => of<IReportListItem[]>([])));
        }),
        tap(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((reports) => this.allReports.set(reports));
  }

  /** Headline counts for the stat cards (computed from the full list). */
  readonly stats = computed(() => {
    const reports = this.allReports();
    return {
      total: reports.length,
      completed: reports.filter((report) => report.status === 'Completed').length,
      critical: reports.filter((report) => report.riskLevel === 'Critical').length,
      breaking: reports.filter((report) => report.breakingApiCount > 0).length,
    };
  });

  private readonly filtered = computed(() => {
    const term = this.search().trim().toLowerCase();
    const status = this.status();
    return this.allReports().filter((report) => {
      const matchesStatus = status === 'all' || report.status === status;
      const matchesTerm =
        !term ||
        report.name.toLowerCase().includes(term) ||
        report.runATitle.toLowerCase().includes(term) ||
        report.runBTitle.toLowerCase().includes(term);
      return matchesStatus && matchesTerm;
    });
  });

  readonly totalCount = computed(() => this.filtered().length);

  readonly reports = computed(() => {
    const start = (this.pageNumber() - 1) * this.pageSize();
    return this.filtered().slice(start, start + this.pageSize());
  });

  /** Generates a report via the API, refreshing the list; emits the created report to the caller. */
  generate(payload: IReportGenerate): Observable<IReportListItem> {
    return this.api.generate(payload).pipe(
      tap(() => {
        this.pageNumber.set(1);
        this.refresh();
      }),
      takeUntilDestroyed(this.destroyRef),
    );
  }

  /** Deletes a report (and its stored AI analysis), then refreshes the list. */
  remove(id: number): Observable<void> {
    return this.api.delete(id).pipe(
      tap(() => this.refresh()),
      takeUntilDestroyed(this.destroyRef),
    );
  }

  setSearch(term: string): void {
    this.search.set(term);
    this.pageNumber.set(1);
  }

  setStatus(status: ReportStatusFilter): void {
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

/** Formats the numeric id as the display id shown in the UI (e.g. REP-018). */
export function displayReportId(id: number): string {
  return `REP-${String(id).padStart(3, '0')}`;
}
