import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  ApexAxisChartSeries,
  ApexChart,
  ApexDataLabels,
  ApexGrid,
  ApexLegend,
  ApexMarkers,
  ApexStroke,
  ApexTooltip,
  ApexXAxis,
  ApexYAxis,
  NgApexchartsModule,
} from 'ng-apexcharts';
import { catchError, of } from 'rxjs';

import { ReportApiService } from '../../apis/report-api.service';

/** Compact chart options for the Summary "Changes over time (Response Time)" panel. */
interface ITrendOptions {
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
}

const COLOR_A = '#6366f1';
const COLOR_B = '#f59e0b';

/**
 * "Changes over time (Response Time)" — a compact response-time line (Run A vs Run B, per request)
 * for the report Summary tab. Renders the chart body only; the surrounding card + title live in the
 * Summary panel. Fetches from the report performance endpoint.
 */
@Component({
  selector: 'app-report-response-trend',
  standalone: true,
  imports: [NgApexchartsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './report-response-trend.component.html',
})
export class ReportResponseTrendComponent {
  readonly reportId = input.required<number>();

  private readonly reportApi = inject(ReportApiService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);

  private readonly runATitle = signal('Run A');
  private readonly runBTitle = signal('Run B');
  private readonly rows = signal<{ label: string; title: string; a: number | null; b: number | null }[]>([]);

  protected readonly hasData = computed(() => this.rows().length > 0);

  constructor() {
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
        this.rows.set(
          performance.perRequest.map((row, index) => ({
            label: this.shortLabel(row.title, index),
            title: row.title || `${row.method} ${row.endpoint}`,
            a: row.durationMsA,
            b: row.durationMsB,
          })),
        );
      });
  }

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

  protected readonly chart = computed<ITrendOptions>(() => {
    const rows = this.rows();
    return {
      series: [
        { name: this.runATitle(), data: rows.map((row) => row.a) },
        { name: this.runBTitle(), data: rows.map((row) => row.b) },
      ],
      chart: { type: 'line', height: 240, fontFamily: 'inherit', toolbar: { show: false }, zoom: { enabled: false }, animations: { enabled: false } },
      xaxis: {
        categories: rows.map((row) => row.label),
        labels: { show: false },
        tickPlacement: 'on',
        axisBorder: { show: false },
        axisTicks: { show: false },
      },
      yaxis: { labels: { formatter: (value: number) => `${Math.round(value)}ms`, style: { colors: '#64748b' } } },
      stroke: { curve: 'smooth', width: 2 },
      markers: { size: 0, hover: { size: 4 } },
      dataLabels: { enabled: false },
      legend: { position: 'bottom', horizontalAlign: 'center', fontSize: '12px' },
      grid: { borderColor: '#e2e8f0', strokeDashArray: 4, padding: { left: 8, right: 8 } },
      tooltip: {
        shared: true,
        intersect: false,
        x: { formatter: (_value: number, opts?: { dataPointIndex: number }) => rows[opts?.dataPointIndex ?? 0]?.title ?? '' },
        y: { formatter: (value: number) => `${Math.round(value)} ms` },
      },
      colors: [COLOR_A, COLOR_B],
    };
  });
}
