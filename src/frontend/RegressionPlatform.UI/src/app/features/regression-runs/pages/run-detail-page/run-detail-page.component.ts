import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, debounceTime, distinctUntilChanged, of, switchMap, tap } from 'rxjs';

import { IPagedResult } from '@core/models';
import { AppIconsConstants } from '@core/constants';
import {
  CellTemplateDirective,
  DataTableComponent,
  IDataTableColumn,
  ISelectOption,
  InputComponent,
  PaginatorComponent,
  SelectComponent,
  StatusBadgeComponent,
  StatusTone,
} from '@shared/components';

import { IApiRunStatistic, IRunDetail, IRunResponseItem, RunStatus } from '../../models/run.model';
import { RunApiService } from '../../apis/run-api.service';
import { displayRunId, formatDuration, resultStatusLabel } from '../../services/run.service';
import { RunResultsComponent } from '../../components/run-results/run-results.component';

type RunDetailTab = 'summary' | 'apis' | 'requests' | 'results' | 'logs';

/** Per-API health for the APIs tab (worst outcome wins). */
type ApiHealth = 'Passed' | 'Warning' | 'Failed';

type ApiHealthFilter = 'all' | ApiHealth;

interface IRunTab {
  key: RunDetailTab;
  label: string;
}

/** Distribution slice percentages + a conic-gradient for the CSS donut. */
interface IRunDistribution {
  pass: number;
  warning: number;
  fail: number;
  total: number;
  passPercent: number;
  warningPercent: number;
  failPercent: number;
  gradient: string;
}

/** Run detail — Summary tab (API statistics, status distribution, run info). */
@Component({
  selector: 'app-run-detail-page',
  standalone: true,
  imports: [
    DatePipe,
    DecimalPipe,
    ReactiveFormsModule,
    MatIconModule,
    InputComponent,
    SelectComponent,
    StatusBadgeComponent,
    DataTableComponent,
    CellTemplateDirective,
    PaginatorComponent,
    RunResultsComponent,
    RouterLink,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './run-detail-page.component.html',
})
export class RunDetailPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(RunApiService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly displayRunId = displayRunId;
  protected readonly formatDuration = formatDuration;
  protected readonly resultStatusLabel = resultStatusLabel;
  protected readonly searchIcon = AppIconsConstants.search;

  /** Icons for the header metadata chips. */
  protected readonly metaIcons = {
    environment: AppIconsConstants.environment,
    runType: AppIconsConstants.runType,
    start: AppIconsConstants.schedule,
    duration: AppIconsConstants.duration,
    api: AppIconsConstants.apis,
  };

  // APIs-tab filters.
  protected readonly apiSearchControl = new FormControl<string>('', { nonNullable: true });
  protected readonly apiStatusControl = new FormControl<ApiHealthFilter>('all', { nonNullable: true });
  private readonly apiSearch = signal('');
  private readonly apiStatusFilter = signal<ApiHealthFilter>('all');
  // APIs-tab sort (client-side, over the fully-loaded statistics).
  protected readonly apiSort = signal<string | null>(null);
  protected readonly apiSortDir = signal<'asc' | 'desc'>('asc');

  protected readonly apiStatusOptions: ISelectOption[] = [
    { value: 'all', label: 'All' },
    { value: 'Passed', label: 'Success' },
    { value: 'Warning', label: 'Warning' },
    { value: 'Failed', label: 'Failed' },
  ];

  /** API filter options (All + one per API in the run) for the Requests/Results tabs. */
  protected readonly apiFilterOptions = computed<ISelectOption[]>(() => [
    { value: 'all', label: 'All APIs' },
    ...(this.detail()?.apiStatistics ?? []).map((stat) => ({
      value: String(stat.apiId),
      label: `${stat.method} ${stat.apiName}`,
    })),
  ]);

  // Requests-tab state (server-side paged + filter).
  protected readonly requestSearchControl = new FormControl<string>('', { nonNullable: true });
  protected readonly requestStatusControl = new FormControl<ApiHealthFilter>('all', { nonNullable: true });
  protected readonly requestApiControl = new FormControl<string>('all', { nonNullable: true });
  private readonly requestSearch = signal('');
  private readonly requestStatus = signal<ApiHealthFilter>('all');
  private readonly requestApiId = signal<number | null>(null);
  protected readonly requestPage = signal(1);
  // Requests-tab sort (server-side; sent with the paged query).
  protected readonly requestSort = signal<string | null>(null);
  protected readonly requestSortDir = signal<'asc' | 'desc'>('asc');

  private static readonly emptyResponses: IPagedResult<IRunResponseItem> = {
    items: [],
    pageNumber: 1,
    pageSize: 20,
    totalCount: 0,
    totalPages: 0,
  };
  protected readonly responses = signal<IPagedResult<IRunResponseItem>>(RunDetailPageComponent.emptyResponses);
  /** True while the Requests tab's paged query is loading (search / status / API / paging). */
  protected readonly responsesLoading = signal(false);

  /** Run id from the route (used to load detail + passed to child tabs like Results). */
  protected readonly runId = Number(this.route.snapshot.paramMap.get('runId'));

  protected readonly detail = signal<IRunDetail | null>(null);
  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly activeTab = signal<RunDetailTab>('summary');

  /** Fixed-length arrays for the loading skeleton's @for placeholders. */
  protected readonly skeletonChips = [0, 1, 2, 3, 4];
  protected readonly skeletonTabs = [0, 1, 2, 3, 4];
  protected readonly skeletonRows = [0, 1, 2, 3, 4];

  protected readonly tabs: IRunTab[] = [
    { key: 'summary', label: 'Summary' },
    { key: 'apis', label: 'APIs' },
    { key: 'requests', label: 'Requests' },
    { key: 'results', label: 'Results' },
    { key: 'logs', label: 'Logs' },
  ];

  protected readonly columns: IDataTableColumn[] = [
    { key: 'apiName', label: 'Api' },
    { key: 'requestCount', label: 'Requests', align: 'right', widthClass: 'w-[100px]' },
    { key: 'passedCount', label: 'Success', align: 'right', widthClass: 'w-[90px]', headerClass: 'text-green-600' },
    { key: 'warningCount', label: 'Warning', align: 'right', widthClass: 'w-[90px]', headerClass: 'text-yellow-600' },
    { key: 'failedCount', label: 'Failed', align: 'right', widthClass: 'w-[90px]', headerClass: 'text-red-700' },
    { key: 'successRate', label: 'Success Rate', align: 'right', widthClass: 'w-[110px]' },
    { key: 'avgResponseTimeMs', label: 'Avg Response Time', align: 'right', widthClass: 'w-[140px]' },
  ];

  /** Full per-API table for the APIs tab (method + endpoint + per-API status). Sorted client-side. */
  protected readonly apiColumns: IDataTableColumn[] = [
    { key: 'apiName', label: 'Api', sortable: true },
    { key: 'endpoint', label: 'Endpoint', widthClass: 'w-[220px]', sortable: true },
    { key: 'requestCount', label: 'Requests', align: 'right', widthClass: 'w-[100px]', sortable: true },
    { key: 'passedCount', label: 'Success', align: 'right', widthClass: 'w-[90px]', headerClass: 'text-green-600', sortable: true },
    { key: 'warningCount', label: 'Warning', align: 'right', widthClass: 'w-[90px]', headerClass: 'text-yellow-600', sortable: true },
    { key: 'failedCount', label: 'Failed', align: 'right', widthClass: 'w-[90px]', headerClass: 'text-red-700', sortable: true },
    { key: 'successRate', label: 'Success Rate', align: 'right', widthClass: 'w-[110px]', sortable: true },
    { key: 'avgResponseTimeMs', label: 'Avg Response Time', align: 'right', widthClass: 'w-[140px]', sortable: true },
    { key: 'apiStatus', label: 'Status', align: 'center', widthClass: 'w-[110px]', sortable: true },
  ];

  /** Per-request table for the Requests tab. Sorted server-side (paged). */
  protected readonly requestColumns: IDataTableColumn[] = [
    { key: 'title', label: 'Request', sortable: true },
    { key: 'endpoint', label: 'Api', widthClass: 'w-[240px]', sortable: true },
    { key: 'status', label: 'Status', align: 'center', widthClass: 'w-[110px]', sortable: true },
    { key: 'statusCode', label: 'Code', align: 'right', widthClass: 'w-[80px]', sortable: true },
    { key: 'durationMs', label: 'Response Time', align: 'right', widthClass: 'w-[130px]', sortable: true },
    { key: 'responseSize', label: 'Size', align: 'right', widthClass: 'w-[100px]', sortable: true },
    { key: 'createdDate', label: 'Executed', widthClass: 'w-[160px]', sortable: true },
  ];

  private static readonly statusTones: Record<RunStatus, StatusTone> = {
    Completed: 'success',
    Failed: 'error',
    Running: 'info',
    Queued: 'info',
    Pending: 'neutral',
    Cancelled: 'warning',
  };

  private static readonly apiHealthTones: Record<ApiHealth, StatusTone> = {
    Passed: 'success',
    Warning: 'warning',
    Failed: 'error',
  };

  /** APIs filtered by search (name/endpoint) + per-API status. */
  protected readonly filteredApis = computed<IApiRunStatistic[]>(() => {
    const run = this.detail();
    if (!run) {
      return [];
    }
    const term = this.apiSearch().trim().toLowerCase();
    const status = this.apiStatusFilter();
    const filtered = run.apiStatistics.filter((stat) => {
      const matchesTerm =
        !term || stat.apiName.toLowerCase().includes(term) || stat.endpoint.toLowerCase().includes(term);
      const matchesStatus = status === 'all' || this.apiHealth(stat) === status;
      return matchesTerm && matchesStatus;
    });

    const sortKey = this.apiSort();
    if (!sortKey) {
      return filtered;
    }
    const dir = this.apiSortDir() === 'asc' ? 1 : -1;
    return [...filtered].sort((a, b) => {
      const va = this.apiSortValue(a, sortKey);
      const vb = this.apiSortValue(b, sortKey);
      return va < vb ? -dir : va > vb ? dir : 0;
    });
  });

  /**
   * Status distribution for the donut — summed from the per-API statistics (same source as the
   * API Statistics table) so the two always agree. Null when nothing was executed.
   */
  protected readonly distribution = computed<IRunDistribution | null>(() => {
    const run = this.detail();
    if (!run) {
      return null;
    }
    const pass = run.apiStatistics.reduce((sum, stat) => sum + stat.passedCount, 0);
    const warning = run.apiStatistics.reduce((sum, stat) => sum + stat.warningCount, 0);
    const fail = run.apiStatistics.reduce((sum, stat) => sum + stat.failedCount, 0);
    const total = pass + warning + fail;
    if (total === 0) {
      return null;
    }
    const passPercent = (pass / total) * 100;
    const warningPercent = (warning / total) * 100;
    const failPercent = (fail / total) * 100;
    const passEnd = passPercent;
    const warningEnd = passPercent + warningPercent;
    return {
      pass,
      warning,
      fail,
      total,
      passPercent: Math.round(passPercent),
      warningPercent: Math.round(warningPercent),
      failPercent: Math.round(failPercent),
      gradient: `conic-gradient(#22c55e 0 ${passEnd}%, #f59e0b ${passEnd}% ${warningEnd}%, #ef4444 ${warningEnd}% 100%)`,
    };
  });

  constructor() {
    const runId = this.runId;
    this.loadDetail();

    this.apiSearchControl.valueChanges
      .pipe(debounceTime(250), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((term) => this.apiSearch.set(term));

    this.apiStatusControl.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((status) => this.apiStatusFilter.set(status));

    // Requests tab: reactive paged load (search/status/page) — switchMap cancels stale requests.
    const responseQuery = computed(() => ({
      page: this.requestPage(),
      search: this.requestSearch(),
      status: this.requestStatus(),
      apiId: this.requestApiId(),
      sortBy: this.requestSort(),
      sortDirection: this.requestSortDir(),
    }));
    toObservable(responseQuery)
      .pipe(
        tap(() => this.responsesLoading.set(true)),
        switchMap((query) =>
          this.api
            .getResponses(runId, {
              pageNumber: query.page,
              pageSize: RunDetailPageComponent.emptyResponses.pageSize,
              search: query.search.trim() || undefined,
              status: query.status,
              apiId: query.apiId,
              sortBy: query.sortBy,
              sortDirection: query.sortDirection,
            })
            .pipe(catchError(() => of(RunDetailPageComponent.emptyResponses))),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((page) => {
        this.responses.set(page);
        this.responsesLoading.set(false);
      });

    this.requestSearchControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((term) => {
        this.requestSearch.set(term);
        this.requestPage.set(1);
      });
    this.requestStatusControl.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((status) => {
      this.requestStatus.set(status);
      this.requestPage.set(1);
    });
    this.requestApiControl.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((value) => {
      this.requestApiId.set(value === 'all' ? null : Number(value));
      this.requestPage.set(1);
    });
  }

  /** Loads (or reloads) the run's Summary detail (API statistics, distribution, counts). */
  private loadDetail(): void {
    this.api
      .getById(this.runId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (detail) => {
          this.detail.set(detail);
          this.loading.set(false);
        },
        error: () => {
          this.errorMessage.set('Failed to load run details.');
          this.loading.set(false);
        },
      });
  }

  /** After a request is re-run in the Results tab, reload the Summary so its API stats + counts refresh. */
  protected onResultRerun(): void {
    this.loadDetail();
  }

  /** Sort key → comparable value for a per-API row (client-side APIs-tab sort). */
  private apiSortValue(stat: IApiRunStatistic, key: string): string | number {
    switch (key) {
      case 'apiName':
        return stat.apiName.toLowerCase();
      case 'endpoint':
        return stat.endpoint.toLowerCase();
      case 'requestCount':
        return stat.requestCount;
      case 'passedCount':
        return stat.passedCount;
      case 'warningCount':
        return stat.warningCount;
      case 'failedCount':
        return stat.failedCount;
      case 'successRate':
        return stat.requestCount > 0 ? stat.passedCount / stat.requestCount : -1;
      case 'avgResponseTimeMs':
        return stat.avgResponseTimeMs ?? -1;
      case 'apiStatus': {
        const health = this.apiHealth(stat);
        return health === 'Failed' ? 2 : health === 'Warning' ? 1 : 0;
      }
      default:
        return 0;
    }
  }

  /** Toggle the APIs-tab sort: same column flips direction, a new column starts ascending. */
  onApiSort(key: string): void {
    if (this.apiSort() === key) {
      this.apiSortDir.update((dir) => (dir === 'asc' ? 'desc' : 'asc'));
    } else {
      this.apiSort.set(key);
      this.apiSortDir.set('asc');
    }
  }

  /** Toggle the Requests-tab sort (server-side); reset to page 1 so results start from the top. */
  onRequestSort(key: string): void {
    if (this.requestSort() === key) {
      this.requestSortDir.update((dir) => (dir === 'asc' ? 'desc' : 'asc'));
    } else {
      this.requestSort.set(key);
      this.requestSortDir.set('asc');
    }
    this.requestPage.set(1);
  }

  /** Worst-outcome status for an API: any failed → Failed; else any warning → Warning; else Passed. */
  protected apiHealth(stat: IApiRunStatistic): ApiHealth {
    if (stat.failedCount > 0) {
      return 'Failed';
    }
    return stat.warningCount > 0 ? 'Warning' : 'Passed';
  }

  protected apiHealthTone(stat: IApiRunStatistic): StatusTone {
    return RunDetailPageComponent.apiHealthTones[this.apiHealth(stat)];
  }

  protected asResponse(row: unknown): IRunResponseItem {
    return row as IRunResponseItem;
  }

  /** Tone for a per-request status (Passed/Warning/Failed). */
  protected responseStatusTone(status: string): StatusTone {
    return RunDetailPageComponent.apiHealthTones[status as ApiHealth] ?? 'neutral';
  }

  protected formatSize(bytes: number | null): string {
    if (bytes === null) {
      return '—';
    }
    return bytes < 1024 ? `${bytes} B` : `${(bytes / 1024).toFixed(1)} KB`;
  }

  onRequestPage(page: number): void {
    this.requestPage.set(page);
  }

  protected asStatistic(row: unknown): IApiRunStatistic {
    return row as IApiRunStatistic;
  }

  protected statusTone(status: RunStatus): StatusTone {
    return RunDetailPageComponent.statusTones[status];
  }

  protected formatMs(ms: number | null): string {
    return ms === null ? '—' : `${Math.round(ms)} ms`;
  }

  /** % of an API's requests that passed (responded OK and fast). */
  protected successRate(stat: IApiRunStatistic): string {
    return stat.requestCount > 0 ? `${((stat.passedCount / stat.requestCount) * 100).toFixed(1)}%` : '—';
  }
}
