import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  computed,
  inject,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { CdkVirtualScrollViewport, ScrollingModule } from '@angular/cdk/scrolling';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { catchError, debounceTime, distinctUntilChanged, of, switchMap, tap } from 'rxjs';

import { IPagedResult } from '@core/models';
import { AppIconsConstants } from '@core/constants';
import { ISelectOption, InputComponent, SelectComponent, StatusBadgeComponent, StatusTone } from '@shared/components';

import {
  CompareResultFilter,
  CompareResultType,
  ICompareApiStatistic,
  ICompareChangeSummary,
  ICompareDetailDiff,
  ICompareDetailItem,
  IFieldDiff,
} from '../../models/compare.model';
import { CompareApiService } from '../../apis/compare-api.service';
import { DiffRowType, IDiffRow, buildLineDiff } from './line-diff';

/** A diff row with the line text pre-highlighted for [innerHTML]. */
interface IDiffRowView extends IDiffRow {
  leftHtml: string | null;
  rightHtml: string | null;
}

/** Master/detail diff viewer for a compare: per-request list + aligned, colored side-by-side diff. */
@Component({
  selector: 'app-compare-diff',
  standalone: true,
  imports: [ReactiveFormsModule, ScrollingModule, MatIconModule, InputComponent, SelectComponent, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './compare-diff.component.html',
})
export class CompareDiffComponent {
  /** The compare whose details are shown. */
  readonly compareId = input.required<number>();
  /** Column labels for the two sides (run names). */
  readonly runALabel = input<string>('Run A');
  readonly runBLabel = input<string>('Run B');
  /** APIs in this compare (for the API filter dropdown). */
  readonly apis = input<ICompareApiStatistic[]>([]);
  /** Distinct change patterns (from AI Insights) — powers the "filter by change" dropdown. */
  readonly changeSummary = input<ICompareChangeSummary | null>(null);

  private readonly api = inject(CompareApiService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly diffScroll = viewChild<ElementRef<HTMLElement>>('diffScroll');

  protected readonly searchIcon = AppIconsConstants.search;
  protected readonly prevIcon = AppIconsConstants.expandLess;
  protected readonly nextIcon = AppIconsConstants.expandMore;

  private static readonly listPageSize = 100;
  private static readonly highlightMaxChars = 100_000;

  protected readonly resultOptions: ISelectOption[] = [
    { value: 'all', label: 'All' },
    { value: 'Changed', label: 'Changed' },
    { value: 'Added', label: 'Added' },
    { value: 'Removed', label: 'Removed' },
    { value: 'Failed', label: 'Failed' },
    { value: 'Unchanged', label: 'Unchanged' },
  ];

  protected readonly searchControl = new FormControl<string>('', { nonNullable: true });
  protected readonly resultControl = new FormControl<CompareResultFilter>('all', { nonNullable: true });
  /** API filter ('all' or the apiId as a string). */
  protected readonly apiControl = new FormControl<string>('all', { nonNullable: true });
  /** Change-pattern filter ('all' or "apiIndex:patternIndex" into the change summary). */
  protected readonly patternControl = new FormControl<string>('all', { nonNullable: true });

  private readonly searchTerm = toSignal(
    this.searchControl.valueChanges.pipe(debounceTime(300), distinctUntilChanged()),
    { initialValue: '' },
  );
  private readonly resultFilter = toSignal(this.resultControl.valueChanges, { initialValue: 'all' as CompareResultFilter });
  private readonly apiFilter = toSignal(this.apiControl.valueChanges, { initialValue: 'all' });
  private readonly patternKey = toSignal(this.patternControl.valueChanges, { initialValue: 'all' });

  /** Options for the change-pattern dropdown, built from the AI-Insights change summary. */
  protected readonly patternOptions = computed<ISelectOption[]>(() => {
    const summary = this.changeSummary();
    const options: ISelectOption[] = [{ value: 'all', label: 'All changes' }];
    if (!summary) {
      return options;
    }
    const multiApi = summary.apis.length > 1;
    summary.apis.forEach((api, apiIndex) => {
      api.patterns.forEach((pattern, patternIndex) => {
        const prefix = multiApi ? `${api.method} ${api.apiName} · ` : '';
        options.push({
          value: `${apiIndex}:${patternIndex}`,
          label: `${prefix}${pattern.changeType}: ${pattern.path} (${pattern.occurrences})`,
        });
      });
    });
    return options;
  });

  /** The (changeType, path) of the selected pattern, or null when "All changes". */
  private readonly selectedPattern = computed<{ changeType: string; path: string } | null>(() => {
    const key = this.patternKey();
    if (!key || key === 'all') {
      return null;
    }
    const [apiIndex, patternIndex] = key.split(':').map(Number);
    const pattern = this.changeSummary()?.apis[apiIndex]?.patterns[patternIndex];
    return pattern ? { changeType: pattern.changeType, path: pattern.path } : null;
  });

  /** Dropdown options: "All APIs" + one per API in this compare. */
  protected readonly apiOptions = computed<ISelectOption[]>(() => [
    { value: 'all', label: 'All APIs' },
    ...this.apis().map((api) => ({ value: String(api.apiId), label: `${api.method} ${api.apiName}` })),
  ]);

  protected readonly results = signal<ICompareDetailItem[]>([]);
  protected readonly listLoading = signal(true);
  /** True while appending the next page (infinite scroll). */
  protected readonly loadingMore = signal(false);
  /** Total matching requests (for the header count + knowing when to stop paging). */
  protected readonly totalCount = signal(0);
  protected readonly selectedId = signal<number | null>(null);
  protected readonly detail = signal<ICompareDetailDiff | null>(null);
  protected readonly detailLoading = signal(false);

  /** More pages available to load (drives the infinite-scroll fetch). */
  protected readonly hasMore = computed(() => this.results().length < this.totalCount());
  /** The virtual-scroll viewport (to know how far the user has scrolled). */
  private readonly listViewport = viewChild(CdkVirtualScrollViewport);
  /** Fixed row height (px) for the virtual list — must match the item's rendered height. */
  protected readonly itemSize = 68;
  private loadedPage = 1;

  private static readonly emptyPage: IPagedResult<ICompareDetailItem> = {
    items: [], pageNumber: 1, pageSize: CompareDiffComponent.listPageSize, totalCount: 0, totalPages: 0,
  };

  /** Index into changeRowIndices() of the currently-focused change (-1 = none yet). */
  private readonly currentChange = signal(-1);

  private static readonly tones: Record<CompareResultType, StatusTone> = {
    Unchanged: 'neutral',
    Changed: 'warning',
    Added: 'success',
    Removed: 'error',
    Failed: 'error',
  };

  constructor() {
    // Left list: reload when compare/search/result changes; switchMap cancels stale requests.
    const listQuery = computed(() => ({
      compareId: this.compareId(),
      search: this.searchTerm().trim(),
      result: this.resultFilter(),
      apiId: this.apiFilter() === 'all' ? null : Number(this.apiFilter()),
      pattern: this.selectedPattern(),
    }));
    toObservable(listQuery)
      .pipe(
        tap(() => {
          this.listLoading.set(true);
          this.loadedPage = 1; // filters changed → back to page 1 (replace, not append)
        }),
        switchMap((query) =>
          this.api
            .getDetails(query.compareId, {
              pageNumber: 1,
              pageSize: CompareDiffComponent.listPageSize,
              search: query.search || undefined,
              result: query.result,
              apiId: query.apiId,
              changeType: query.pattern?.changeType ?? null,
              changePath: query.pattern?.path ?? null,
            })
            .pipe(catchError(() => of(CompareDiffComponent.emptyPage))),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((page) => {
        const items = page.items;
        this.results.set(items);
        this.totalCount.set(page.totalCount);
        this.listLoading.set(false);
        // Scroll back to the top when the filter set changes.
        this.listViewport()?.scrollToIndex(0);
        const current = this.selectedId();
        if (current === null || !items.some((item) => item.requestId === current)) {
          this.selectedId.set(items[0]?.requestId ?? null);
        }
      });

    // Right panel: load the selected request's diff reactively.
    const detailQuery = computed(() => ({ compareId: this.compareId(), requestId: this.selectedId() }));
    toObservable(detailQuery)
      .pipe(
        switchMap((query) => {
          if (query.requestId === null) {
            return of(null);
          }
          this.detailLoading.set(true);
          // No startWith(null): keep the previous diff visible while loading so the panel doesn't
          // collapse (which would shrink the page and bounce the outer scroll to the top).
          return this.api.getDetailDiff(query.compareId, query.requestId).pipe(catchError(() => of(null)));
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((detail) => {
        this.detail.set(detail);
        this.detailLoading.set(false);
        // Reset the change cursor for the newly-loaded request (avoids a stale position like "2/1").
        this.currentChange.set(-1);
      });
  }

  protected select(id: number): void {
    this.selectedId.set(id);
  }

  /** Load the next page and append it (infinite scroll). */
  protected loadMore(): void {
    if (this.listLoading() || this.loadingMore() || !this.hasMore()) {
      return;
    }
    this.loadingMore.set(true);
    const nextPage = this.loadedPage + 1;
    const pattern = this.selectedPattern();
    this.api
      .getDetails(this.compareId(), {
        pageNumber: nextPage,
        pageSize: CompareDiffComponent.listPageSize,
        search: this.searchTerm().trim() || undefined,
        result: this.resultFilter(),
        apiId: this.apiFilter() === 'all' ? null : Number(this.apiFilter()),
        changeType: pattern?.changeType ?? null,
        changePath: pattern?.path ?? null,
      })
      .pipe(
        catchError(() => of(CompareDiffComponent.emptyPage)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((page) => {
        this.loadedPage = nextPage;
        this.results.update((current) => [...current, ...page.items]);
        this.totalCount.set(page.totalCount);
        this.loadingMore.set(false);
      });
  }

  /** Virtual-scroll range changed → fetch the next page when the user nears the end. */
  protected onListScroll(): void {
    const viewport = this.listViewport();
    if (!viewport) {
      return;
    }
    const end = viewport.getRenderedRange().end;
    const total = viewport.getDataLength();
    if (total > 0 && end >= total - 15) {
      this.loadMore();
    }
  }

  protected trackByRequestId(_: number, item: ICompareDetailItem): number {
    return item.requestId;
  }

  protected resultTone(result: CompareResultType): StatusTone {
    return CompareDiffComponent.tones[result];
  }

  /** Parsed field-level diffs for the selected request (empty when none / unparseable). */
  protected readonly fieldDiffs = computed<IFieldDiff[]>(() => {
    const json = this.detail()?.diffJson;
    if (!json) {
      return [];
    }
    try {
      const parsed = JSON.parse(json);
      return Array.isArray(parsed) ? (parsed as IFieldDiff[]) : [];
    } catch {
      return [];
    }
  });

  /** Aligned, colored, pre-highlighted side-by-side rows for the selected request. */
  protected readonly diffRows = computed<IDiffRowView[]>(() => {
    const detail = this.detail();
    if (!detail) {
      return [];
    }
    const fullA = detail.responseAJson ? this.prettyLines(detail.responseAJson) : [];
    const fullB = detail.responseBJson ? this.prettyLines(detail.responseBJson) : [];

    // How the rules (ignore / cut-at) change each full line — shown but not counted when they explain the difference.
    const rulesA = this.ruleLineMap(detail.responseAJson, detail.responseAComparedJson);
    const rulesB = this.ruleLineMap(detail.responseBJson, detail.responseBComparedJson);

    return buildLineDiff(fullA, fullB).map((row) => {
      const removedByRule =
        (row.leftNo !== null && rulesA.removed.has(row.leftNo)) || (row.rightNo !== null && rulesB.removed.has(row.rightNo));
      // A changed line whose two sides become identical once the rules are applied (e.g. a signed URL
      // cut at "?") differs only in the ignored part → ignored (blue), not a real change (yellow).
      const equalAfterRules =
        row.type === 'changed' &&
        row.leftNo !== null &&
        row.rightNo !== null &&
        rulesA.mapped.has(row.leftNo) &&
        rulesA.mapped.get(row.leftNo) === rulesB.mapped.get(row.rightNo) &&
        (rulesA.mapped.get(row.leftNo) !== row.left || rulesB.mapped.get(row.rightNo) !== row.right);
      const isIgnored = removedByRule || equalAfterRules;
      return {
        ...row,
        type: isIgnored ? 'ignored' : row.type,
        leftHtml: row.left === null ? null : this.highlightLine(row.left),
        rightHtml: row.right === null ? null : this.highlightLine(row.right),
      };
    });
  });

  /** Row indices (into diffRows) that are real changes — the navigation targets (ignored excluded). */
  protected readonly changeRowIndices = computed<number[]>(() =>
    this.diffRows().reduce<number[]>((acc, row, index) => {
      if (row.type !== 'equal' && row.type !== 'ignored') {
        acc.push(index);
      }
      return acc;
    }, []),
  );

  /**
   * How the compare rules affect each line of the full body — derived by diffing the full body
   * against the rule-applied one: `removed` = lines an ignore rule drops; `mapped` = full line number →
   * its rule-applied text (identical, or altered e.g. by a "cut value at" rule).
   */
  private ruleLineMap(full: string | null, compared: string | null): { removed: Set<number>; mapped: Map<number, string> } {
    const removed = new Set<number>();
    const mapped = new Map<number, string>();
    if (!full || compared === null || compared === undefined) {
      return { removed, mapped };
    }
    for (const row of buildLineDiff(this.prettyLines(full), this.prettyLines(compared))) {
      if (row.leftNo === null) {
        continue;
      }
      if (row.type === 'removed') {
        removed.add(row.leftNo);
      } else if (row.right !== null) {
        mapped.set(row.leftNo, row.right);
      }
    }
    return { removed, mapped };
  }

  protected readonly changeCount = computed(() => this.changeRowIndices().length);
  protected readonly currentPosition = computed(() => {
    const total = this.changeRowIndices().length;
    if (total === 0) {
      return 0;
    }
    // Before any navigation (-1) show 1; otherwise the 1-based index, clamped to the total.
    return Math.min(Math.max(this.currentChange() + 1, 1), total);
  });
  protected readonly hasIgnoredRows = computed(() => this.diffRows().some((row) => row.type === 'ignored'));

  protected next(): void {
    this.move(1);
  }

  protected prev(): void {
    this.move(-1);
  }

  private move(delta: number): void {
    const indices = this.changeRowIndices();
    if (indices.length === 0) {
      return;
    }
    let next = this.currentChange() + delta;
    if (next < 0) {
      next = indices.length - 1;
    } else if (next >= indices.length) {
      next = 0;
    }
    this.currentChange.set(next);

    const container = this.diffScroll()?.nativeElement;
    const target = container?.querySelector<HTMLElement>(`[data-row="${indices[next]}"]`);
    if (!container || !target) {
      return;
    }
    // Scroll only the diff container (not ancestors like scrollIntoView would): center the row.
    const containerRect = container.getBoundingClientRect();
    const targetRect = target.getBoundingClientRect();
    const offset = targetRect.top - containerRect.top - (container.clientHeight - target.clientHeight) / 2;
    container.scrollTo({ top: container.scrollTop + offset, behavior: 'smooth' });
  }

  /** Left-side line background by row type (changed/removed colored; added = gap; ignored = blue). */
  protected leftRowClass(type: DiffRowType): string {
    switch (type) {
      case 'changed':
        return 'bg-amber-50 border-l-2 border-amber-400';
      case 'removed':
        return 'bg-red-50 border-l-2 border-red-400';
      case 'ignored':
        return 'bg-sky-50 border-l-2 border-sky-300';
      case 'added':
        return 'bg-slate-50 border-l-2 border-transparent';
      default:
        return 'border-l-2 border-transparent';
    }
  }

  /** Right-side line background by row type (changed/added colored; removed = gap; ignored = blue). */
  protected rightRowClass(type: DiffRowType): string {
    switch (type) {
      case 'changed':
        return 'bg-amber-50 border-l-2 border-amber-400';
      case 'added':
        return 'bg-green-50 border-l-2 border-green-400';
      case 'ignored':
        return 'bg-sky-50 border-l-2 border-sky-300';
      case 'removed':
        return 'bg-slate-50 border-l-2 border-transparent';
      default:
        return 'border-l-2 border-transparent';
    }
  }

  /** Renders a diff value (old/new) compactly for the changes list. */
  protected renderValue(value: unknown): string {
    if (value === undefined) {
      return '—';
    }
    return typeof value === 'string' ? value : JSON.stringify(value);
  }

  /** U+00A0 NO-BREAK SPACE — the invisible IP watermark marker (see IABB-340). */
  private static readonly watermarkChar = String.fromCharCode(160); // U+00A0 NO-BREAK SPACE

  /**
   * True when a value change differs ONLY by watermark characters (U+00A0 vs a normal space) — i.e. the
   * "difference" is the invisible IP watermark on the Fix Name, not a real content change. Detected by
   * checking a watermark char is present and that replacing every U+00A0 with a normal space makes the
   * two strings identical.
   */
  protected isWatermarkDiff(diff: IFieldDiff): boolean {
    if (diff.changeType !== 'ValueChanged' || typeof diff.old !== 'string' || typeof diff.new !== 'string') {
      return false;
    }
    if (diff.old === diff.new) {
      return false;
    }
    const wm = CompareDiffComponent.watermarkChar;
    if (!diff.old.includes(wm) && !diff.new.includes(wm)) {
      return false;
    }
    return diff.old.split(wm).join(' ') === diff.new.split(wm).join(' ');
  }

  private prettyLines(json: string): string[] {
    let text: string;
    try {
      text = JSON.stringify(JSON.parse(json), null, 2);
    } catch {
      text = json;
    }
    return text.split('\n');
  }

  private escapeHtml(text: string): string {
    return text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
  }

  /** One line of JSON → HTML-escaped markup with token colors (keys/strings/numbers/booleans/null). */
  private highlightLine(line: string): string {
    const escaped = this.escapeHtml(line);
    if (line.length > CompareDiffComponent.highlightMaxChars) {
      return escaped;
    }
    const tokenPattern =
      /("(?:\\u[\da-fA-F]{4}|\\[^u]|[^\\"])*"(\s*:)?|\b(?:true|false)\b|\bnull\b|-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)/g;
    return escaped.replace(tokenPattern, (match) => {
      let cls = 'text-amber-700';
      if (match.startsWith('&quot;') || match.startsWith('"')) {
        cls = /:\s*$/.test(match) ? 'text-rose-700' : 'text-blue-700';
      } else if (match === 'true' || match === 'false') {
        cls = 'text-purple-700';
      } else if (match === 'null') {
        cls = 'text-slate-400';
      }
      return `<span class="${cls}">${match}</span>`;
    });
  }
}
