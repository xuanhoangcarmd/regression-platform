import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  ApexAxisChartSeries,
  ApexChart,
  ApexDataLabels,
  ApexFill,
  ApexGrid,
  ApexLegend,
  ApexMarkers,
  ApexPlotOptions,
  ApexStroke,
  ApexTooltip,
  ApexXAxis,
  ApexYAxis,
  NgApexchartsModule,
} from 'ng-apexcharts';
import { catchError, of } from 'rxjs';

import { ReportApiService } from '../../apis/report-api.service';

/** Full set of ApexCharts options we drive (subset of ApexOptions we actually set). */
interface IChartOptions {
  series: ApexAxisChartSeries;
  chart: ApexChart;
  xaxis: ApexXAxis;
  yaxis: ApexYAxis;
  stroke: ApexStroke;
  markers: ApexMarkers;
  dataLabels: ApexDataLabels;
  legend: ApexLegend;
  grid: ApexGrid;
  tooltip: ApexTooltip;
  colors: string[];
  plotOptions: ApexPlotOptions;
  fill: ApexFill;
}

/** Run A / Run B colours (indigo / amber) — kept consistent across both charts. */
const COLOR_A = '#6366f1';
const COLOR_B = '#f59e0b';

/**
 * Response-time charts for a report: Run A vs Run B, from the backend performance endpoint
 * (`/api/reports/{id}/performance`). Loads its own data when mounted.
 */
@Component({
  selector: 'app-report-performance-charts',
  standalone: true,
  imports: [NgApexchartsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './report-performance-charts.component.html',
})
export class ReportPerformanceChartsComponent {
  readonly reportId = input.required<number>();

  private readonly reportApi = inject(ReportApiService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly runATitle = signal('Run A');
  protected readonly runBTitle = signal('Run B');

  /** Per-request durations (both runs), request order preserved. */
  private readonly perRequest = signal<{ apiId: number; apiName: string; label: string; title: string; a: number | null; b: number | null }[]>([]);
  /** Per-API average durations. */
  private readonly perApi = signal<{ label: string; a: number | null; b: number | null }[]>([]);

  /** API filter for the "Response Time by Request" chart (null = all APIs; default). */
  protected readonly selectedApiId = signal<number | null>(null);

  /** APIs that have per-request timing (for the filter dropdown), in request order. */
  protected readonly requestApis = computed(() => {
    const seen = new Map<number, string>();
    for (const row of this.perRequest()) {
      if (!seen.has(row.apiId)) {
        seen.set(row.apiId, row.apiName);
      }
    }
    return [...seen].map(([id, name]) => ({ id, name }));
  });

  /** Rows for the request chart, filtered by the selected API (all when none selected). */
  private readonly filteredRequests = computed(() => {
    const apiId = this.selectedApiId();
    const rows = this.perRequest();
    return apiId === null ? rows : rows.filter((row) => row.apiId === apiId);
  });

  protected onApiFilterChange(value: string): void {
    this.selectedApiId.set(value === '' ? null : Number(value));
  }

  /** Hard cap on points drawn in the "Response Time by Request" chart. */
  private static readonly maxChartPoints = 200;

  /**
   * Rows actually plotted: the filtered requests capped to {@link maxChartPoints} by even sampling
   * (pick evenly-spaced points across the set) so a large run stays readable and the x-axis isn't
   * overcrowded. Returns the rows unchanged when already within the cap.
   */
  private readonly chartRows = computed(() => {
    const rows = this.filteredRequests();
    const max = ReportPerformanceChartsComponent.maxChartPoints;
    if (rows.length <= max) {
      return rows;
    }
    const sampled: typeof rows = [];
    for (let i = 0; i < max; i++) {
      sampled.push(rows[Math.floor((i * rows.length) / max)]);
    }
    return sampled;
  });

  constructor() {
    // Load once, when the charts are first rendered (the tab mounts this component lazily).
    queueMicrotask(() => this.load());
  }

  private load(): void {
    this.reportApi
      .getPerformance(this.reportId())
      .pipe(
        catchError(() => of(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((performance) => {
        this.loading.set(false);
        if (performance === null) {
          this.errorMessage.set('Failed to load response-time data.');
          return;
        }
        this.runATitle.set(performance.runATitle);
        this.runBTitle.set(performance.runBTitle);
        this.perRequest.set(
          performance.perRequest.map((row, index) => ({
            apiId: row.apiId,
            apiName: row.apiName,
            label: this.shortLabel(row.title, index),
            title: row.title || `${row.method} ${row.endpoint}`,
            a: row.durationMsA,
            b: row.durationMsB,
          })),
        );
        this.perApi.set(
          performance.perApi.map((row) => ({ label: row.apiName, a: row.avgDurationMsA, b: row.avgDurationMsB })),
        );
      });
  }

  /**
   * A compact axis label for a request. Request titles look like `[VIN] - <guid>`; the GUID is
   * noise on the axis, so prefer the bracketed identifier (e.g. the VIN), then the text before
   * the dash, and finally a sequential `#n`.
   */
  private shortLabel(title: string | null, index: number): string {
    const bracketed = title?.match(/^\[([^\]]+)\]/);
    if (bracketed) {
      return bracketed[1];
    }
    const dash = title?.indexOf(' - ') ?? -1;
    if (title && dash > 0) {
      return title.slice(0, dash);
    }
    return title?.trim() || `#${index + 1}`;
  }

  protected readonly hasRequestData = computed(() => this.filteredRequests().length > 0);
  protected readonly hasApiData = computed(() => this.perApi().length > 0);

  /** Line chart: response time per request, Run A vs Run B (filtered by API, capped to 200 points). */
  protected readonly requestChart = computed<IChartOptions>(() => {
    const rows = this.chartRows();
    return {
      series: [
        { name: this.runATitle(), data: rows.map((row) => row.a) },
        { name: this.runBTitle(), data: rows.map((row) => row.b) },
      ],
      chart: { type: 'line', height: 320, fontFamily: 'inherit', toolbar: { show: false }, zoom: { enabled: false }, animations: { enabled: false } },
      xaxis: {
        categories: rows.map((row) => row.label),
        // Per-request labels are noise on the axis (the request title shows in the tooltip instead).
        labels: { show: false },
        tickPlacement: 'on',
        axisBorder: { show: false },
        axisTicks: { show: false },
      },
      yaxis: { labels: { formatter: (value: number) => `${Math.round(value)} ms`, style: { colors: '#64748b' } } },
      stroke: { curve: 'smooth', width: 2 },
      markers: { size: 3, hover: { size: 5 } },
      dataLabels: { enabled: false },
      legend: { position: 'top', horizontalAlign: 'right', fontSize: '12px' },
      grid: { borderColor: '#e2e8f0', strokeDashArray: 4 },
      tooltip: {
        shared: true,
        intersect: false,
        x: { formatter: (_value: number, opts?: { dataPointIndex: number }) => rows[opts?.dataPointIndex ?? 0]?.title ?? '' },
        y: { formatter: (value: number) => `${Math.round(value)} ms` },
      },
      colors: [COLOR_A, COLOR_B],
      plotOptions: {},
      fill: { type: 'solid' },
    };
  });

  /** Grouped-bar chart: average response time per API, Run A vs Run B. */
  protected readonly apiChart = computed<IChartOptions>(() => {
    const rows = this.perApi();
    return {
      series: [
        { name: this.runATitle(), data: rows.map((row) => row.a) },
        { name: this.runBTitle(), data: rows.map((row) => row.b) },
      ],
      chart: { type: 'bar', height: 320, fontFamily: 'inherit', toolbar: { show: false }, animations: { enabled: false } },
      xaxis: {
        categories: rows.map((row) => row.label),
        labels: { style: { fontSize: '11px', colors: '#64748b' }, trim: true, hideOverlappingLabels: false },
        axisBorder: { show: false },
        axisTicks: { show: false },
      },
      yaxis: { labels: { formatter: (value: number) => `${Math.round(value)} ms`, style: { colors: '#64748b' } } },
      stroke: { show: true, width: 2, colors: ['transparent'] },
      markers: {},
      // Average shown on top of each column (no hover needed); empty for missing values.
      dataLabels: {
        enabled: true,
        formatter: (value: number) => (value ? `${Math.round(value)} ms` : ''),
        offsetY: -18,
        style: { fontSize: '10px', fontWeight: 600, colors: ['#475569'] },
      },
      legend: { position: 'top', horizontalAlign: 'right', fontSize: '12px' },
      grid: { borderColor: '#e2e8f0', strokeDashArray: 4 },
      tooltip: { shared: true, intersect: false, y: { formatter: (value: number) => `${Math.round(value)} ms` } },
      colors: [COLOR_A, COLOR_B],
      plotOptions: {
        bar: {
          horizontal: false,
          columnWidth: '55%',
          borderRadius: 4,
          borderRadiusApplication: 'end',
          dataLabels: { position: 'top' },
        },
      },
      fill: { type: 'solid' },
    };
  });
}
