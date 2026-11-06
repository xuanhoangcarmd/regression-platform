import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, Router } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';

import { AppIconsConstants } from '@core/constants';
import {
  ButtonComponent,
  CellTemplateDirective,
  DataTableComponent,
  IDataTableColumn,
  ISelectOption,
  InputComponent,
  ModalComponent,
  PaginatorComponent,
  SelectComponent,
  StatusBadgeComponent,
  StatusTone,
} from '@shared/components';

import { displayCompareId } from '@features/compare/services/compare.service';

import { IReportListItem, ReportRiskLevel, ReportStatus, ReportStatusFilter } from '../../models/report.model';
import { ReportService, displayReportId } from '../../services/report.service';
import { ReportGenerateModalComponent } from '../modals/report-generate-modal/report-generate-modal.component';

/** A score band (badge label + colour) derived from the regression score. */
interface IScoreBand {
  label: string;
  textClass: string;
  barClass: string;
}

/** Reports: generated regression reports for the active project (1 report = 1 compare). */
@Component({
  selector: 'app-report-list-page',
  standalone: true,
  imports: [
    DatePipe,
    DecimalPipe,
    ReactiveFormsModule,
    MatIconModule,
    ButtonComponent,
    InputComponent,
    SelectComponent,
    DataTableComponent,
    CellTemplateDirective,
    StatusBadgeComponent,
    PaginatorComponent,
    ReportGenerateModalComponent,
    ModalComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './report-list-page.component.html',
})
export class ReportListPageComponent {
  private readonly service = inject(ReportService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly reports = this.service.reports;
  protected readonly loading = this.service.loading;
  protected readonly totalCount = this.service.totalCount;
  protected readonly pageNumber = this.service.pageNumber;
  protected readonly pageSize = this.service.pageSize;
  protected readonly stats = this.service.stats;

  protected readonly generateOpen = signal(false);
  protected readonly generating = signal(false);
  /** The report pending deletion (drives the confirm modal); null = closed. */
  protected readonly reportToDelete = signal<IReportListItem | null>(null);
  /** True while the delete request is in flight (spinner + disabled confirm). */
  protected readonly deleting = signal(false);

  protected readonly displayReportId = displayReportId;
  protected readonly displayCompareId = displayCompareId;

  protected readonly addIcon = AppIconsConstants.add;
  protected readonly searchIcon = AppIconsConstants.search;
  protected readonly viewIcon = AppIconsConstants.view;
  protected readonly deleteIcon = AppIconsConstants.delete;
  protected readonly arrowIcon = AppIconsConstants.arrowForward;
  protected readonly reportIcon = AppIconsConstants.reports;
  protected readonly warningIcon = AppIconsConstants.warning;
  protected readonly successIcon = AppIconsConstants.success;

  protected readonly searchControl = new FormControl<string>('', { nonNullable: true });
  protected readonly statusControl = new FormControl<ReportStatusFilter>('all', { nonNullable: true });

  protected readonly statusOptions: ISelectOption[] = [
    { value: 'all', label: 'All Statuses' },
    { value: 'Completed', label: 'Completed' },
  ];

  protected readonly columns: IDataTableColumn[] = [
    { key: 'id', label: 'Report', widthClass: 'w-[110px]' },
    { key: 'compare', label: 'Compare', widthClass: 'w-[120px]' },
    { key: 'runs', label: 'Runs (A → B)' },
    { key: 'score', label: 'Regression Score', widthClass: 'w-[200px]' },
    { key: 'risk', label: 'Risk', align: 'center', widthClass: 'w-[100px]' },
    { key: 'status', label: 'Status', widthClass: 'w-[120px]' },
    { key: 'generatedDate', label: 'Generated', widthClass: 'w-[160px]' },
    { key: 'actions', label: 'Actions', align: 'center', widthClass: 'w-[80px]' },
  ];

  private static readonly statusTones: Record<ReportStatus, StatusTone> = {
    Completed: 'success',
  };

  constructor() {
    this.searchControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((term) => this.service.setSearch(term));

    this.statusControl.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe((status) => this.service.setStatus(status));
  }

  protected asReport(row: unknown): IReportListItem {
    return row as IReportListItem;
  }

  protected statusTone(status: ReportStatus): StatusTone {
    return ReportListPageComponent.statusTones[status];
  }

  /** Score band (label + colours) — higher score = healthier regression. */
  protected scoreBand(score: number): IScoreBand {
    if (score >= 95) {
      return { label: 'Excellent', textClass: 'text-green-600', barClass: 'bg-green-500' };
    }
    if (score >= 85) {
      return { label: 'Good', textClass: 'text-green-600', barClass: 'bg-green-500' };
    }
    if (score >= 70) {
      return { label: 'Warning', textClass: 'text-amber-600', barClass: 'bg-amber-500' };
    }
    return { label: 'Critical', textClass: 'text-red-700', barClass: 'bg-red-500' };
  }

  /** Colour classes for the risk-level badge. */
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

  onPage(page: number): void {
    this.service.setPage(page);
  }

  /** Generate (or regenerate) a report for the selected compare, then open its detail. */
  onGenerate(compareId: number): void {
    this.generating.set(true);
    this.service.generate({ compareId }).subscribe({
      next: (report) => {
        this.generating.set(false);
        this.generateOpen.set(false);
        this.router.navigate([report.id], { relativeTo: this.route });
      },
      error: () => this.generating.set(false),
    });
  }

  openDetail(row: unknown): void {
    this.router.navigate([this.asReport(row).id], { relativeTo: this.route });
  }

  /** Open the delete confirmation for a report. */
  askDelete(row: unknown): void {
    this.reportToDelete.set(this.asReport(row));
  }

  cancelDelete(): void {
    this.reportToDelete.set(null);
  }

  /** Delete the report pending confirmation; keep the confirm locked/loading until it finishes. */
  confirmDelete(): void {
    const report = this.reportToDelete();
    if (!report || this.deleting()) {
      return;
    }
    this.deleting.set(true);
    this.service.remove(report.id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.reportToDelete.set(null); // close only after the delete succeeds
      },
      error: () => this.deleting.set(false), // keep the modal open so the user can retry
    });
  }
}
