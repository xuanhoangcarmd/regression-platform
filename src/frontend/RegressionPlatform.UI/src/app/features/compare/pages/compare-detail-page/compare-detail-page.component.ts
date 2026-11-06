import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute } from '@angular/router';
import { catchError, interval, of, switchMap } from 'rxjs';

import { AppIconsConstants } from '@core/constants';
import {
  ButtonComponent,
  CellTemplateDirective,
  DataTableComponent,
  IDataTableColumn,
  StatusBadgeComponent,
  StatusTone,
} from '@shared/components';

import {
  CompareStatus,
  ICompareApiStatistic,
  ICompareChangeSummary,
  ICompareSummary,
} from '../../models/compare.model';
import { CompareApiService } from '../../apis/compare-api.service';
import { displayCompareId } from '../../services/compare.service';
import { CompareDiffComponent } from '../../components/compare-diff/compare-diff.component';

type CompareTab = 'summary' | 'differences' | 'changes';

/** A slice of the result distribution (for the donut + legend). */
interface IDistributionSlice {
  label: string;
  count: number;
  percent: number;
  color: string;
  textClass: string;
}

@Component({
  selector: 'app-compare-detail-page',
  standalone: true,
  imports: [DatePipe, DecimalPipe, MatIconModule, ButtonComponent, DataTableComponent, CellTemplateDirective, StatusBadgeComponent, CompareDiffComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './compare-detail-page.component.html',
})
export class CompareDetailPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(CompareApiService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly displayCompareId = displayCompareId;
  protected readonly compareIcon = AppIconsConstants.compare;

  protected readonly compareId = Number(this.route.snapshot.paramMap.get('compareId'));

  protected readonly summary = signal<ICompareSummary | null>(null);
  protected readonly apiStatistics = signal<ICompareApiStatistic[]>([]);
  protected readonly changeSummary = signal<ICompareChangeSummary | null>(null);
  protected readonly loading = signal(true);
  /** True while the per-API statistics + change summary are being fetched (after the summary loads). */
  protected readonly statsLoading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly activeTab = signal<CompareTab>('summary');
  protected readonly copied = signal(false);

  protected readonly copyIcon = AppIconsConstants.copy;
  protected readonly checkIcon = AppIconsConstants.success;

  protected readonly copiedPrompt = signal(false);

  /** Pretty-printed change-summary JSON (what you'd hand to an AI). */
  protected readonly changeSummaryJson = computed(() => {
    const summary = this.changeSummary();
    return summary ? JSON.stringify(summary, null, 2) : '';
  });

  /** A ready-to-paste AI prompt: context + the analysis ask + the change-summary JSON. */
  private static readonly promptIntro = [
    'You are a QA engineer reviewing an automotive diagnostic REGRESSION comparison between two software builds.',
    '',
    "Run A = the BASELINE (previous build); Run B = the NEW build. The JSON below summarizes how Run B's",
    'responses differ from Run A\'s. Changes are grouped PER API under "apis" — each item has apiName,',
    'method, endpoint, requestCount, and its own "patterns". Each pattern has:',
    '- changeType: ValueChanged / FieldAdded / FieldRemoved / TypeChanged / OrderChanged (strict-order compares: key order differs)',
    '- path: a JSON path (array indices normalized to [*])',
    '- occurrences (total hits), requestCount (requests affected), distinctTransitions (distinct old→new pairs)',
    '- samples: a few representative old → new values',
    '',
    'Classify each pattern as NOISE (version/firmware bump, value formatting e.g. "0.0"→"0.00", array',
    'reordering, timestamps) or a REAL change / likely regression.',
    '',
    'Summarize SEPARATELY FOR EACH API that changed — do not merge APIs together. Within an API,',
    'group by DATA AREA (Freeze Frame, Monitors, added fields, removed fields, …) and synthesize —',
    'DO NOT relist JSON paths; one short line per area with a brief example.',
    '',
    'Respond in EXACTLY this Markdown format — concise, email-ready, no preamble, no extra sections,',
    'under ~80 words PER API, headings bold exactly as written:',
    '',
    '**Verdict:** Identical | Minor differences | Significant differences — <one sentence overall>',
    '',
    'Then, for EACH API that changed, one block:',
    '',
    '**<METHOD endpoint>** (<apiName>)',
    '- <area>: <what changed + example>',
    '- Noise: <short list, or "None">',
    '',
    '**Conclusion:** <one line naming which APIs/areas changed>',
    '',
    '**Recommended action:** Approve | Needs review — <what to verify>',
    '',
    'Compare data:',
  ].join('\n');

  /** Full prompt (intro + fenced JSON) for one-click paste into an AI. */
  protected readonly changeSummaryPrompt = computed(() =>
    this.changeSummary()
      ? `${CompareDetailPageComponent.promptIntro}\n\n\`\`\`json\n${this.changeSummaryJson()}\n\`\`\`\n`
      : '',
  );

  private readonly refreshTrigger = signal(0);

  /** Fixed-length arrays for the loading skeleton's @for placeholders. */
  protected readonly skeletonTabs = [0, 1, 2];
  protected readonly skeletonCards = [0, 1, 2, 3, 4, 5];
  protected readonly skeletonRows = [0, 1, 2, 3, 4];

  protected readonly tabs: { key: CompareTab; label: string; icon?: string }[] = [
    { key: 'summary', label: 'Summary' },
    { key: 'differences', label: 'Differences' },
    { key: 'changes', label: 'AI Insights', icon: AppIconsConstants.ai },
  ];

  protected readonly apiColumns: IDataTableColumn[] = [
    { key: 'apiName', label: 'Api' },
    { key: 'endpoint', label: 'Endpoint', widthClass: 'w-[220px]' },
    { key: 'requestCount', label: 'Requests', align: 'right', widthClass: 'w-[100px]' },
    { key: 'unchangedCount', label: 'Unchanged', align: 'right', widthClass: 'w-[110px]' },
    { key: 'changedCount', label: 'Changed', align: 'right', widthClass: 'w-[100px]', headerClass: 'text-amber-600' },
    { key: 'addedCount', label: 'Added', align: 'right', widthClass: 'w-[90px]', headerClass: 'text-green-600' },
    { key: 'removedCount', label: 'Removed', align: 'right', widthClass: 'w-[100px]', headerClass: 'text-red-700' },
    { key: 'failedCount', label: 'Failed', align: 'right', widthClass: 'w-[90px]', headerClass: 'text-red-700' },
  ];

  private static readonly statusTones: Record<CompareStatus, StatusTone> = {
    Completed: 'success',
    Failed: 'error',
    Running: 'info',
    Pending: 'neutral',
    Cancelled: 'warning',
  };

  constructor() {
    const compareId = this.compareId;

    // Load (and re-load while polling) the summary; pull the API breakdown once finished.
    toObservable(computed(() => ({ id: compareId, refresh: this.refreshTrigger() })))
      .pipe(
        switchMap((query) =>
          this.api.getById(query.id).pipe(catchError(() => of<ICompareSummary | null>(null))),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((summary) => {
        this.loading.set(false);
        if (summary === null) {
          this.errorMessage.set('Failed to load compare details.');
          return;
        }
        this.summary.set(summary);
        if (summary.compareResultId !== null) {
          this.statsLoading.set(true);
          this.api
            .getApiStatistics(compareId)
            .pipe(catchError(() => of<ICompareApiStatistic[]>([])), takeUntilDestroyed(this.destroyRef))
            .subscribe((stats) => {
              this.apiStatistics.set(stats);
              this.statsLoading.set(false);
            });
          this.api
            .getChangeSummary(compareId)
            .pipe(catchError(() => of<ICompareChangeSummary | null>(null)), takeUntilDestroyed(this.destroyRef))
            .subscribe((changes) => this.changeSummary.set(changes));
        }
      });

    // Poll while the compare is still executing.
    interval(4000)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        const status = this.summary()?.status;
        if (status === 'Running' || status === 'Pending') {
          this.refreshTrigger.update((value) => value + 1);
        }
      });
  }

  protected statusTone(status: CompareStatus): StatusTone {
    return CompareDetailPageComponent.statusTones[status];
  }

  protected copyJson(): void {
    void navigator.clipboard?.writeText(this.changeSummaryJson());
    this.copied.set(true);
    setTimeout(() => this.copied.set(false), 1500);
  }

  protected copyPrompt(): void {
    void navigator.clipboard?.writeText(this.changeSummaryPrompt());
    this.copiedPrompt.set(true);
    setTimeout(() => this.copiedPrompt.set(false), 1500);
  }

  protected asStatistic(row: unknown): ICompareApiStatistic {
    return row as ICompareApiStatistic;
  }

  /** Result distribution for the donut + legend (null when nothing was compared). */
  protected readonly distribution = computed<{ total: number; slices: IDistributionSlice[]; gradient: string } | null>(() => {
    const summary = this.summary();
    if (!summary || summary.compareResultId === null) {
      return null;
    }

    const raw = [
      { label: 'Unchanged', count: summary.unchangedCount ?? 0, color: '#94a3b8', textClass: 'text-slate-500' },
      { label: 'Changed', count: summary.changedCount ?? 0, color: '#f59e0b', textClass: 'text-amber-600' },
      { label: 'Added', count: summary.addedCount ?? 0, color: '#22c55e', textClass: 'text-green-600' },
      { label: 'Removed', count: summary.removedCount ?? 0, color: '#ef4444', textClass: 'text-red-700' },
      { label: 'Failed', count: summary.failedCount ?? 0, color: '#b91c1c', textClass: 'text-red-800' },
    ];
    const total = raw.reduce((sum, slice) => sum + slice.count, 0);
    if (total === 0) {
      return null;
    }

    const slices: IDistributionSlice[] = raw.map((slice) => ({
      ...slice,
      percent: Math.round((slice.count / total) * 100),
    }));

    // Build the conic-gradient stops from the raw (unrounded) shares so segments add to 100%.
    let cursor = 0;
    const stops = raw
      .filter((slice) => slice.count > 0)
      .map((slice) => {
        const start = (cursor / total) * 100;
        cursor += slice.count;
        const end = (cursor / total) * 100;
        return `${slice.color} ${start}% ${end}%`;
      });

    return { total, slices, gradient: `conic-gradient(${stops.join(', ')})` };
  });
}
