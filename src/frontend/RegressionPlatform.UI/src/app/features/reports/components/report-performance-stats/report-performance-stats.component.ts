import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  ApexAxisChartSeries,
  ApexChart,
  ApexDataLabels,
  ApexFill,
  ApexGrid,
  ApexLegend,
  ApexPlotOptions,
  ApexStroke,
  ApexTooltip,
  ApexXAxis,
  ApexYAxis,
  NgApexchartsModule,
} from 'ng-apexcharts';
import { catchError, of } from 'rxjs';

import { ReportApiService } from '../../apis/report-api.service';
import { IReportPerfApiStat, IReportPerformanceStats } from '../../models/report.model';

/** A per-API table row with pre-formatted cells + the avg Δ%. */
interface IApiRow {
  method: string;
  apiName: string;
  p95: string;
  max: string;
  min: string;
  size: string;
  delta: string | null;
  tone: 'good' | 'bad' | 'neutral';
}

interface IHistogramOptions {
  series: ApexAxisChartSeries;
  chart: ApexChart;
  xaxis: ApexXAxis;
  yaxis: ApexYAxis;
  stroke: ApexStroke;
  dataLabels: ApexDataLabels;
  legend: ApexLegend;
  grid: ApexGrid;
  tooltip: ApexTooltip;
  colors: string[];
  plotOptions: ApexPlotOptions;
  fill: ApexFill;
}

const COLOR_A = '#6366f1';
const COLOR_B = '#f59e0b';

/**
 * Extra performance statistics for the report Distribution tab: a latency histogram (Run A vs Run B)
 * and a per-API table with dimensions not shown elsewhere (p95, max, avg size, Δ% of the average).
 * Loads from `/api/reports/{id}/performance-stats`.
 */
@Component({
  selector: 'app-report-performance-stats',
  standalone: true,
  imports: [NgApexchartsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './report-performance-stats.component.html',
})
export class ReportPerformanceStatsComponent {
  readonly reportId = input.required<number>();

  private readonly reportApi = inject(ReportApiService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly stats = signal<IReportPerformanceStats | null>(null);

  constructor() {
    queueMicrotask(() => this.load());
  }

  private load(): void {
    this.reportApi
      .getPerformanceStats(this.reportId())
      .pipe(
        catchError(() => of(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((stats) => {
        this.loading.set(false);
        if (stats === null) {
          this.errorMessage.set('Failed to load performance statistics.');
          return;
        }
        this.stats.set(stats);
      });
  }

  protected readonly runATitle = computed(() => this.stats()?.runATitle ?? 'Run A');
  protected readonly runBTitle = computed(() => this.stats()?.runBTitle ?? 'Run B');

  protected readonly hasHistogram = computed(() => (this.stats()?.histogram.bins.length ?? 0) > 0);
  protected readonly hasApiRows = computed(() => this.apiRows().length > 0);

  /** Per-API table rows (p95 / max / size as "A → B", plus Δ% of the average). */
  protected readonly apiRows = computed<IApiRow[]>(() =>
    (this.stats()?.perApi ?? []).map((api) => this.toRow(api)),
  );

  private toRow(api: IReportPerfApiStat): IApiRow {
    return {
      method: api.method,
      apiName: api.apiName,
      p95: `${this.formatMs(api.p95MsA)} → ${this.formatMs(api.p95MsB)}`,
      max: `${this.formatMs(api.maxMsA)} → ${this.formatMs(api.maxMsB)}`,
      min: `${this.formatMs(api.minMsA)} → ${this.formatMs(api.minMsB)}`,
      size: `${this.formatBytes(api.avgSizeA)} → ${this.formatBytes(api.avgSizeB)}`,
      delta: this.deltaPercent(api.avgMsA, api.avgMsB),
      tone: this.lowerIsBetterTone(api.avgMsA, api.avgMsB),
    };
  }

  private lowerIsBetterTone(a: number | null, b: number | null): 'good' | 'bad' | 'neutral' {
    if (a === null || b === null || a === b) {
      return 'neutral';
    }
    return b > a ? 'bad' : 'good';
  }

  private deltaPercent(a: number | null, b: number | null): string | null {
    if (a === null || b === null || a === 0) {
      return null;
    }
    const pct = Math.round(((b - a) / a) * 100);
    return pct === 0 ? null : `${pct > 0 ? '+' : ''}${pct}%`;
  }

  protected formatMs(value: number | null): string {
    if (value === null) {
      return '—';
    }
    return value >= 1000 ? `${(value / 1000).toFixed(2)} s` : `${value} ms`;
  }

  private formatBytes(value: number | null): string {
    if (value === null) {
      return '—';
    }
    if (value < 1024) {
      return `${value} B`;
    }
    if (value < 1024 * 1024) {
      return `${(value / 1024).toFixed(1)} KB`;
    }
    return `${(value / (1024 * 1024)).toFixed(1)} MB`;
  }

  /** Grouped-bar latency histogram: request counts per duration bin, Run A vs Run B. */
  protected readonly histogramChart = computed<IHistogramOptions>(() => {
    const bins = this.stats()?.histogram.bins ?? [];
    return {
      series: [
        { name: this.runATitle(), data: bins.map((bin) => bin.countA) },
        { name: this.runBTitle(), data: bins.map((bin) => bin.countB) },
      ],
      chart: { type: 'bar', height: 300, fontFamily: 'inherit', toolbar: { show: false }, animations: { enabled: false } },
      xaxis: {
        categories: bins.map((bin) => `${bin.fromMs}–${bin.toMs}`),
        title: { text: 'Response time (ms)', style: { fontSize: '11px', color: '#64748b' } },
        labels: { rotate: -45, rotateAlways: false, hideOverlappingLabels: true, style: { fontSize: '11px', colors: '#64748b' } },
        axisBorder: { show: false },
        axisTicks: { show: false },
      },
      yaxis: { labels: { formatter: (value: number) => `${Math.round(value)}`, style: { colors: '#64748b' } }, title: { text: 'Requests', style: { fontSize: '11px', color: '#64748b' } } },
      stroke: { show: true, width: 2, colors: ['transparent'] },
      dataLabels: { enabled: false },
      legend: { position: 'top', horizontalAlign: 'right', fontSize: '12px' },
      grid: { borderColor: '#e2e8f0', strokeDashArray: 4 },
      tooltip: { shared: true, intersect: false, y: { formatter: (value: number) => `${value} req` } },
      colors: [COLOR_A, COLOR_B],
      plotOptions: { bar: { horizontal: false, columnWidth: '70%', borderRadius: 2 } },
      fill: { type: 'solid' },
    };
  });
}
