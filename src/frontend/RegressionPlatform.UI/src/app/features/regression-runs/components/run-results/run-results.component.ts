import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { catchError, debounceTime, distinctUntilChanged, of, startWith, switchMap, tap } from 'rxjs';

import { AppIconsConstants } from '@core/constants';
import { ISelectOption, InputComponent, SelectComponent, StatusBadgeComponent, StatusTone } from '@shared/components';

import { RunApiService } from '../../apis/run-api.service';
import { IApiRunStatistic, IRunResponseDetail, IRunResponseItem } from '../../models/run.model';
import { resultStatusLabel } from '../../services/run.service';

type ResultStatus = 'Passed' | 'Warning' | 'Failed';
type ResultSubTab = 'response' | 'headers' | 'request';

/**
 * Run Results — master/detail view (executed-request list + selected response).
 * Wired to the backend: the list comes from GET /runs/{id}/responses, the detail
 * (body / headers / request) from GET /runs/{id}/responses/{requestId}.
 */
@Component({
  selector: 'app-run-results',
  standalone: true,
  imports: [ReactiveFormsModule, MatIconModule, InputComponent, SelectComponent, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './run-results.component.html',
})
export class RunResultsComponent {
  /** The run whose responses are shown. */
  readonly runId = input.required<number>();
  /** APIs in this run (for the API filter dropdown). */
  readonly apis = input<IApiRunStatistic[]>([]);
  /** Emitted after a request is re-run, so the parent can refresh the run's summary. */
  readonly rerun = output<void>();

  private readonly api = inject(RunApiService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly resultStatusLabel = resultStatusLabel;
  protected readonly copyIcon = AppIconsConstants.copy;
  protected readonly searchIcon = AppIconsConstants.search;
  protected readonly rerunIcon = AppIconsConstants.refresh;

  /** True while the selected request is being re-run (drives the button spinner + disabled state). */
  protected readonly rerunning = signal(false);

  /** Max executed requests loaded into the left list (no paging in this inspector view). */
  private static readonly listPageSize = 100;

  /** Above this size we skip the (regex) highlighter and show plain escaped text, to stay responsive. */
  private static readonly highlightMaxChars = 100_000;

  protected readonly subTabs: { key: ResultSubTab; label: string }[] = [
    { key: 'response', label: 'Response' },
    { key: 'headers', label: 'Headers' },
    { key: 'request', label: 'Request' },
  ];

  protected readonly activeSubTab = signal<ResultSubTab>('response');
  protected readonly copied = signal(false);

  private static readonly tones: Record<ResultStatus, StatusTone> = {
    Passed: 'success',
    Warning: 'warning',
    Failed: 'error',
  };

  protected readonly searchControl = new FormControl<string>('', { nonNullable: true });
  private readonly searchTerm = toSignal(
    this.searchControl.valueChanges.pipe(debounceTime(300), distinctUntilChanged()),
    { initialValue: '' },
  );

  /** API filter ('all' or the apiId as a string). */
  protected readonly apiControl = new FormControl<string>('all', { nonNullable: true });
  private readonly apiFilter = toSignal(this.apiControl.valueChanges, { initialValue: 'all' });

  /** Status filter (all / Passed / Warning / Failed). */
  protected readonly statusControl = new FormControl<string>('all', { nonNullable: true });
  private readonly statusFilter = toSignal(this.statusControl.valueChanges, { initialValue: 'all' });

  protected readonly statusOptions: ISelectOption[] = [
    { value: 'all', label: 'All' },
    { value: 'Passed', label: 'Success' },
    { value: 'Warning', label: 'Warning' },
    { value: 'Failed', label: 'Failed' },
  ];

  /** Dropdown options: "All APIs" + one per API in this run. */
  protected readonly apiOptions = computed<ISelectOption[]>(() => [
    { value: 'all', label: 'All APIs' },
    ...this.apis().map((stat) => ({ value: String(stat.apiId), label: `${stat.method} ${stat.apiName}` })),
  ]);

  protected readonly results = signal<IRunResponseItem[]>([]);
  protected readonly listLoading = signal(true);
  protected readonly selectedId = signal<number | null>(null);
  protected readonly detail = signal<IRunResponseDetail | null>(null);
  protected readonly detailLoading = signal(false);

  constructor() {
    // Left list: reload when the run or search term changes; switchMap cancels stale requests.
    const listQuery = computed(() => ({
      runId: this.runId(),
      search: this.searchTerm().trim(),
      apiId: this.apiFilter() === 'all' ? null : Number(this.apiFilter()),
      status: this.statusFilter(),
    }));
    toObservable(listQuery)
      .pipe(
        tap(() => this.listLoading.set(true)),
        switchMap((query) =>
          this.api
            .getResponses(query.runId, {
              pageNumber: 1,
              pageSize: RunResultsComponent.listPageSize,
              search: query.search || undefined,
              apiId: query.apiId,
              status: query.status,
            })
            .pipe(catchError(() => of({ items: [] as IRunResponseItem[] }))),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((page) => {
        const items = page.items;
        this.results.set(items);
        this.listLoading.set(false);
        // Keep the current selection if still present; otherwise select the first request.
        const current = this.selectedId();
        if (current === null || !items.some((item) => item.requestId === current)) {
          this.selectedId.set(items[0]?.requestId ?? null);
        }
      });

    // Right panel: load the selected request's detail reactively.
    const detailQuery = computed(() => ({ runId: this.runId(), requestId: this.selectedId() }));
    toObservable(detailQuery)
      .pipe(
        switchMap((query) => {
          if (query.requestId === null) {
            return of(null);
          }
          this.detailLoading.set(true);
          return this.api
            .getResponseDetail(query.runId, query.requestId)
            .pipe(startWith(null), catchError(() => of(null)));
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((detail) => {
        this.detail.set(detail);
        this.detailLoading.set(detail === null && this.selectedId() !== null);
      });
  }

  protected select(id: number): void {
    if (id === this.selectedId()) {
      return;
    }
    this.selectedId.set(id);
    this.activeSubTab.set('response');
    this.copied.set(false);
  }

  protected statusTone(status: ResultStatus): StatusTone {
    return RunResultsComponent.tones[status];
  }

  /**
   * Re-run the selected request, then fetch its fresh result and patch it IN PLACE (right panel +
   * matching left-list row). No loading flag / no null reset, so the view updates without the panel
   * collapsing and jumping.
   */
  protected rerunRequest(): void {
    const requestId = this.selectedId();
    if (requestId === null || this.rerunning()) {
      return;
    }
    const runId = this.runId();
    this.rerunning.set(true);
    this.api
      .rerunRequest(runId, requestId)
      .pipe(
        switchMap(() => this.api.getResponseDetail(runId, requestId)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (detail) => {
          this.rerunning.set(false);
          if (detail) {
            this.detail.set(detail); // swap the shown result without a loading flash
            this.patchListItem(detail); // keep the left-list row's status/metrics in sync
          }
          this.rerun.emit(); // let the parent refresh the run summary
        },
        error: () => this.rerunning.set(false),
      });
  }

  /** Update the matching left-list row's status/metrics from a re-run detail (in place). */
  private patchListItem(detail: IRunResponseDetail): void {
    this.results.update((items) =>
      items.map((item) =>
        item.requestId === detail.requestId
          ? {
              ...item,
              status: detail.status,
              statusCode: detail.statusCode,
              durationMs: detail.durationMs,
              responseSize: detail.responseSize,
              errorMessage: detail.errorMessage,
              createdDate: detail.createdDate,
            }
          : item,
      ),
    );
  }

  /** Methods that carry their inputs in the URL (query/path), not a body. */
  private static readonly bodylessMethods = new Set(['GET', 'HEAD']);

  /** The raw code shown in the right panel for the active sub-tab (pretty-printed JSON). */
  protected readonly content = computed<string>(() => {
    const detail = this.detail();
    if (!detail) {
      return '';
    }
    if (this.activeSubTab() === 'request') {
      // GET/HEAD carry their inputs in the URL → show the resolved URL (copy-friendly). Methods with a
      // body (POST/PUT/…) keep showing the request body as before.
      return RunResultsComponent.bodylessMethods.has(detail.method.toUpperCase())
        ? this.resolveUrl(detail.endpoint, this.parseObject(detail.requestQueryJson))
        : this.pretty(detail.requestBody);
    }
    const raw = this.activeSubTab() === 'response' ? detail.responseJson : detail.responseHeadersJson;
    return this.pretty(raw);
  });

  /**
   * Builds the relative URL the run actually called: substitutes each {name} placeholder from
   * `params` (case-insensitive, URL-encoded, consumed), then appends the remaining params as the
   * query string. Mirrors the backend's RunExecutor.BuildUrl (baseUrl is environment config, so it
   * is not shown here).
   */
  private resolveUrl(endpoint: string, params: Record<string, unknown> | null): string {
    const path = endpoint ?? '';
    const remaining = new Map<string, unknown>(Object.entries(params ?? {}));

    const resolvedPath = path.replace(/\{([^{}/]+)\}/g, (match, rawName: string) => {
      const name = rawName.trim().toLowerCase();
      for (const key of remaining.keys()) {
        if (key.toLowerCase() === name) {
          const value = remaining.get(key);
          remaining.delete(key);
          return encodeURIComponent(this.toParamString(value));
        }
      }
      return match; // no matching param — leave the placeholder as-is
    });

    if (remaining.size === 0) {
      return resolvedPath;
    }
    const queryString = [...remaining.entries()]
      .map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(this.toParamString(value))}`)
      .join('&');
    return resolvedPath + (resolvedPath.includes('?') ? '&' : '?') + queryString;
  }

  /** A param value as a string (strings as-is; numbers/booleans/objects JSON-stringified — 0 → "0"). */
  private toParamString(value: unknown): string {
    return typeof value === 'string' ? value : JSON.stringify(value);
  }

  /** Parses a JSON object string into a record, or null when empty/not an object. */
  private parseObject(json: string | null): Record<string, unknown> | null {
    if (!json) {
      return null;
    }
    try {
      const parsed = JSON.parse(json);
      return parsed && typeof parsed === 'object' && !Array.isArray(parsed) ? (parsed as Record<string, unknown>) : null;
    } catch {
      return null;
    }
  }

  /** Lines of the active content, for the numbered gutter. */
  protected readonly contentLines = computed(() => this.content().split('\n'));

  /** Active content as syntax-highlighted, HTML-escaped markup (token <span>s) for `[innerHTML]`. */
  protected readonly highlightedHtml = computed<string>(() => this.highlightJson(this.content()));

  protected formatMs(ms: number | null): string {
    return ms === null ? '—' : `${ms} ms`;
  }

  protected formatSize(bytes: number | null): string {
    if (bytes === null) {
      return '—';
    }
    return bytes < 1024 ? `${bytes} B` : `${(bytes / 1024).toFixed(1)} KB`;
  }

  protected copy(): void {
    void navigator.clipboard?.writeText(this.content());
    this.copied.set(true);
  }

  private pretty(json: string | null): string {
    if (!json) {
      return '(empty)';
    }
    try {
      return JSON.stringify(JSON.parse(json), null, 2);
    } catch {
      return json;
    }
  }

  /** Escape HTML-special chars so arbitrary response bodies can't inject markup. */
  private escapeHtml(text: string): string {
    return text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
  }

  /**
   * Turn (pretty-printed) JSON into HTML-escaped markup with token <span>s.
   * Keys / strings / numbers / booleans / null get distinct colors; punctuation stays default.
   * Large or non-JSON content is returned escaped but un-colored.
   */
  private highlightJson(content: string): string {
    const escaped = this.escapeHtml(content);
    if (content.length > RunResultsComponent.highlightMaxChars) {
      return escaped;
    }
    const tokenPattern =
      /("(?:\\u[\da-fA-F]{4}|\\[^u]|[^\\"])*"(\s*:)?|\b(?:true|false)\b|\bnull\b|-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)/g;
    return escaped.replace(tokenPattern, (match) => {
      let cls = 'text-amber-700'; // number
      if (match.startsWith('&quot;') || match.startsWith('"')) {
        cls = /:\s*$/.test(match) ? 'text-rose-700' : 'text-blue-700'; // key vs string
      } else if (match === 'true' || match === 'false') {
        cls = 'text-purple-700'; // boolean
      } else if (match === 'null') {
        cls = 'text-slate-400'; // null
      }
      return `<span class="${cls}">${match}</span>`;
    });
  }
}
