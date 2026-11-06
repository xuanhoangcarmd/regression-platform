import { DatePipe, DecimalPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  ViewChild,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatIconModule } from '@angular/material/icon';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, of } from 'rxjs';

import { AppIconsConstants, ProjectRouteConstants, RouteConstants } from '@core/constants';
import { ProjectContextService } from '@core/services';
import { ButtonComponent, StatusBadgeComponent, StatusTone } from '@shared/components';

import { displayCompareId } from '@features/compare/services/compare.service';

import { IReportApiStat, IReportDetail, ReportRiskLevel, ReportStatus } from '../../models/report.model';
import { ReportApiService } from '../../apis/report-api.service';
import { ReportExportService } from '../../services/report-export.service';
import { ReportPdfService } from '../../services/report-pdf.service';
import { displayReportId } from '../../services/report.service';
import { ReportAiAnalysisComponent } from '../../components/report-ai-analysis/report-ai-analysis.component';
import { ReportPerformanceChartsComponent } from '../../components/report-performance-charts/report-performance-charts.component';
import { ReportPerformanceStatsComponent } from '../../components/report-performance-stats/report-performance-stats.component';
import { ReportResponseTrendComponent } from '../../components/report-response-trend/report-response-trend.component';
import { ReportEmailModalComponent } from '../modals/report-email-modal/report-email-modal.component';
import { IReportExportRequest, ReportExportModalComponent } from '../modals/report-export-modal/report-export-modal.component';

type ReportTab = 'summary' | 'ai' | 'distribution' | 'activity';

/** A slice of the result distribution (for the donut + legend). */
interface IDistributionSlice {
  label: string;
  count: number;
  percent: number;
  color: string;
  textClass: string;
}

/** One row of the "Top APIs by change %" table. */
interface ITopApi {
  api: IReportApiStat;
  changePercent: number;
  risk: ReportRiskLevel;
}

/** One coloured segment of a per-API distribution bar. */
interface IDistributionSegment {
  label: string;
  count: number;
  color: string;
  percent: number;
}

/** A per-API row for the Distribution tab (stacked bar + counts). */
interface IApiDistribution {
  api: IReportApiStat;
  total: number;
  segments: IDistributionSegment[];
  changePercent: number;
}

/** Report detail: Summary/stats tab (built) + AI/Distribution/Activity tabs (later phases). */
@Component({
  selector: 'app-report-detail-page',
  standalone: true,
  imports: [
    DatePipe,
    DecimalPipe,
    MatIconModule,
    ButtonComponent,
    StatusBadgeComponent,
    ReportExportModalComponent,
    ReportEmailModalComponent,
    ReportPerformanceChartsComponent,
    ReportPerformanceStatsComponent,
    ReportResponseTrendComponent,
    ReportAiAnalysisComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './report-detail-page.component.html',
})
export class ReportDetailPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(ReportApiService);
  private readonly projectContext = inject(ProjectContextService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly pdf = inject(ReportPdfService);
  private readonly exportService = inject(ReportExportService);
  private readonly sanitizer = inject(DomSanitizer);

  @ViewChild('pdfFrame') private pdfFrame?: ElementRef<HTMLIFrameElement>;

  protected readonly displayReportId = displayReportId;
  protected readonly displayCompareId = displayCompareId;

  protected readonly reportId = Number(this.route.snapshot.paramMap.get('reportId'));

  protected readonly reportData = signal<IReportDetail | null>(null);
  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly activeTab = signal<ReportTab>('summary');

  protected readonly compareIcon = AppIconsConstants.compare;
  protected readonly aiIcon = AppIconsConstants.ai;
  protected readonly warningIcon = AppIconsConstants.warning;
  protected readonly scheduleIcon = AppIconsConstants.schedule;
  protected readonly pdfIcon = AppIconsConstants.pdf;
  protected readonly exportIcon = AppIconsConstants.download;
  protected readonly emailIcon = AppIconsConstants.email;
  protected readonly shareIcon = AppIconsConstants.share;
  protected readonly moreIcon = AppIconsConstants.more;
  protected readonly expandIcon = AppIconsConstants.expandMore;
  protected readonly closeIcon = AppIconsConstants.close;

  protected readonly exportOpen = signal(false);
  protected readonly emailOpen = signal(false);

  /**
   * True while THIS report's export is generating. Read from the export service (a root singleton), so
   * it survives leaving and returning to the page — the status shows again for a still-running export.
   */
  protected readonly exporting = computed(() => this.exportService.exportingReportIds().has(this.reportId));

  /** Runs the export chosen in the modal (which has already closed). Progress is tracked in the service. */
  protected onExport(request: IReportExportRequest): void {
    const report = this.reportData();
    if (!report) {
      return;
    }
    this.exportService
      .export(request.format, report, { projectName: this.projectContext.code() }, request.options)
      .catch((error) => console.error('Export failed', error));
  }

  // Preview-PDF modal state (PDF generated client-side; blob embedded in an iframe).
  protected readonly previewPdfOpen = signal(false);
  protected readonly pdfSafeUrl = signal<SafeResourceUrl | null>(null);
  private pdfBlobUrl: string | null = null;

  /** Header actions that arrive in a later phase (rendered disabled with this hint). */
  protected readonly plannedHint = 'Planned — available in a later phase';

  /** Fixed-length arrays for the loading skeleton's @for placeholders. */
  protected readonly skeletonActions = [0, 1, 2, 3, 4, 5];
  protected readonly skeletonTabs = [0, 1, 2, 3];
  protected readonly skeletonCards = [0, 1, 2, 3, 4, 5];
  protected readonly skeletonRows = [0, 1, 2, 3, 4];

  protected readonly tabs: { key: ReportTab; label: string; icon?: string }[] = [
    { key: 'summary', label: 'Summary' },
    { key: 'ai', label: 'AI Analysis', icon: AppIconsConstants.ai },
    { key: 'distribution', label: 'Distribution' },
    { key: 'activity', label: 'Activity' },
  ];

  private static readonly statusTones: Record<ReportStatus, StatusTone> = {
    Completed: 'success',
  };

  constructor() {
    this.api
      .getById(this.reportId)
      .pipe(catchError(() => of<IReportDetail | null>(null)), takeUntilDestroyed(this.destroyRef))
      .subscribe((report) => {
        this.loading.set(false);
        if (report === null) {
          this.errorMessage.set('Failed to load the report.');
          return;
        }
        this.reportData.set(report);
      });

    this.destroyRef.onDestroy(() => this.revokePdf());
  }

  protected statusTone(status: ReportStatus): StatusTone {
    return ReportDetailPageComponent.statusTones[status];
  }

  /** Score band (label + colours) — higher score = healthier regression. */
  protected readonly scoreBand = computed(() => {
    const score = this.reportData()?.regressionScore ?? 0;
    if (score >= 95) {
      return { label: 'Excellent', textClass: 'text-green-600', ringColor: '#22c55e' };
    }
    if (score >= 85) {
      return { label: 'Good', textClass: 'text-green-600', ringColor: '#22c55e' };
    }
    if (score >= 70) {
      return { label: 'Warning', textClass: 'text-amber-600', ringColor: '#f59e0b' };
    }
    return { label: 'Critical', textClass: 'text-red-700', ringColor: '#ef4444' };
  });

  /** Conic-gradient ring for the score gauge (filled up to the score, track for the rest). */
  protected readonly scoreGradient = computed(() => {
    const score = this.reportData()?.regressionScore ?? 0;
    const color = this.scoreBand().ringColor;
    return `conic-gradient(${color} 0% ${score}%, #e2e8f0 ${score}% 100%)`;
  });

  /** Colour classes for a risk-level badge. */
  protected riskClass(risk: ReportRiskLevel): string {
    switch (risk) {
      case 'Low':
        return 'bg-green-50 text-green-700';
      case 'Medium':
        return 'bg-amber-50 text-amber-700';
      case 'High':
        return 'bg-orange-50 text-orange-700';
      case 'Critical':
        return 'bg-red-50 text-red-700';
    }
  }

  /** Result distribution for the donut + legend (null when nothing was compared). */
  protected readonly distribution = computed<{ total: number; slices: IDistributionSlice[]; gradient: string } | null>(() => {
    const report = this.reportData();
    if (!report) {
      return null;
    }

    const raw = [
      { label: 'Unchanged', count: report.unchangedCount, color: '#22c55e', textClass: 'text-green-600' },
      { label: 'Changed', count: report.changedCount, color: '#fbbf24', textClass: 'text-amber-500' },
      { label: 'Added', count: report.addedCount, color: '#8b5cf6', textClass: 'text-violet-600' },
      { label: 'Removed', count: report.removedCount, color: '#ef4444', textClass: 'text-red-700' },
      { label: 'Failed', count: report.failedCount, color: '#b91c1c', textClass: 'text-red-800' },
    ];
    const total = raw.reduce((sum, slice) => sum + slice.count, 0);
    if (total === 0) {
      return null;
    }

    const slices: IDistributionSlice[] = raw.map((slice) => ({
      ...slice,
      percent: Math.round((slice.count / total) * 100),
    }));

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

  /** Top 5 APIs by change % (change = changed + added + removed), with a per-API risk. */
  protected readonly topApis = computed<ITopApi[]>(() => {
    const apis = this.reportData()?.apis ?? [];
    return apis
      .map((api) => {
        const changes = api.changedCount + api.addedCount + api.removedCount;
        const changePercent = api.requestCount > 0 ? Math.round((changes / api.requestCount) * 100) : 0;
        const risk: ReportRiskLevel = api.breakingCount > 0 ? 'High' : changes > 0 ? 'Medium' : 'Low';
        return { api, changePercent, risk };
      })
      .sort((a, b) => b.changePercent - a.changePercent)
      .slice(0, 5);
  });

  /** APIs with backward-incompatible (breaking) changes, most breaking first. */
  protected readonly breakingApis = computed<IReportApiStat[]>(() =>
    (this.reportData()?.apis ?? [])
      .filter((api) => api.breakingCount > 0)
      .sort((a, b) => b.breakingCount - a.breakingCount),
  );

  /** Legend for the per-API distribution bars (matches the Summary donut colours). */
  protected readonly distributionLegend: { label: string; color: string }[] = [
    { label: 'Unchanged', color: '#22c55e' },
    { label: 'Changed', color: '#fbbf24' },
    { label: 'Added', color: '#8b5cf6' },
    { label: 'Removed', color: '#ef4444' },
    { label: 'Failed', color: '#b91c1c' },
  ];

  /** Per-API result distribution (stacked-bar rows), most-changed APIs first. */
  protected readonly apiDistribution = computed<IApiDistribution[]>(() => {
    const apis = this.reportData()?.apis ?? [];
    return apis
      .map((api) => {
        const raw = [
          { label: 'Unchanged', count: api.unchangedCount, color: '#22c55e' },
          { label: 'Changed', count: api.changedCount, color: '#fbbf24' },
          { label: 'Added', count: api.addedCount, color: '#8b5cf6' },
          { label: 'Removed', count: api.removedCount, color: '#ef4444' },
          { label: 'Failed', count: api.failedCount, color: '#b91c1c' },
        ];
        const total = raw.reduce((sum, segment) => sum + segment.count, 0);
        const changes = api.changedCount + api.addedCount + api.removedCount + api.failedCount;
        const segments: IDistributionSegment[] = raw
          .filter((segment) => segment.count > 0)
          .map((segment) => ({ ...segment, percent: total > 0 ? (segment.count / total) * 100 : 0 }));
        return {
          api,
          total,
          segments,
          changePercent: total > 0 ? Math.round((changes / total) * 100) : 0,
        };
      })
      .sort((a, b) => b.changePercent - a.changePercent);
  });

  /** Percentage of the compare's total that a count represents (for the stat cards). */
  protected statPercent(count: number): number {
    const total = this.reportData()?.totalRequest ?? 0;
    return total > 0 ? Math.round((count / total) * 100) : 0;
  }

  /** The stat-card row (Total + the five result categories), driven from the snapshot counts. */
  protected readonly statCards = computed(() => {
    const report = this.reportData();
    if (!report) {
      return [];
    }
    return [
      { label: 'Total Requests', value: report.totalRequest, icon: 'receipt_long', iconWrap: 'bg-blue-50 text-blue-600', percentClass: '', showPercent: false },
      { label: 'Unchanged', value: report.unchangedCount, icon: 'check_circle', iconWrap: 'bg-green-50 text-green-600', percentClass: 'text-green-600', showPercent: true },
      { label: 'Changed', value: report.changedCount, icon: 'change_circle', iconWrap: 'bg-amber-50 text-amber-600', percentClass: 'text-amber-600', showPercent: true },
      { label: 'Added', value: report.addedCount, icon: 'add_circle', iconWrap: 'bg-violet-50 text-violet-600', percentClass: 'text-violet-600', showPercent: true },
      { label: 'Removed', value: report.removedCount, icon: 'do_not_disturb_on', iconWrap: 'bg-red-50 text-red-600', percentClass: 'text-red-600', showPercent: true },
      { label: 'Failed', value: report.failedCount, icon: 'block', iconWrap: 'bg-slate-800 text-white', percentClass: 'text-red-600', showPercent: true },
    ];
  });

  /** Project display name for the Overview panel (route code, upper-cased). */
  protected readonly projectName = computed(() => this.projectContext.code());

  /** Build the PDF, embed it in the preview modal's iframe. */
  protected openPreviewPdf(): void {
    const report = this.reportData();
    if (!report) {
      return;
    }
    this.revokePdf();
    this.pdfBlobUrl = this.pdf.buildObjectUrl(report, { projectName: this.projectContext.code() });
    this.pdfSafeUrl.set(this.sanitizer.bypassSecurityTrustResourceUrl(this.pdfBlobUrl));
    this.previewPdfOpen.set(true);
  }

  protected closePreviewPdf(): void {
    this.previewPdfOpen.set(false);
  }

  /** Save the generated PDF to disk. */
  protected downloadPdf(): void {
    const report = this.reportData();
    if (!this.pdfBlobUrl || !report) {
      return;
    }
    const anchor = document.createElement('a');
    anchor.href = this.pdfBlobUrl;
    anchor.download = this.pdf.fileName(report);
    anchor.click();
  }

  /** Print the PDF shown in the preview iframe. */
  protected printPdf(): void {
    const frame = this.pdfFrame?.nativeElement;
    frame?.contentWindow?.focus();
    frame?.contentWindow?.print();
  }

  private revokePdf(): void {
    if (this.pdfBlobUrl) {
      URL.revokeObjectURL(this.pdfBlobUrl);
      this.pdfBlobUrl = null;
    }
    this.pdfSafeUrl.set(null);
  }

  /** Navigate to the source compare's detail page. */
  protected viewCompare(): void {
    const report = this.reportData();
    if (!report) {
      return;
    }
    this.router.navigate([
      '/',
      RouteConstants.projects,
      this.projectContext.code(),
      ProjectRouteConstants.compare,
      report.compareJobId,
    ]);
  }
}
