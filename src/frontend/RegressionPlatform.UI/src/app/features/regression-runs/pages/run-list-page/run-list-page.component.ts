import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, Router } from '@angular/router';
import { debounceTime, distinctUntilChanged, interval } from 'rxjs';

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

import { IRun, IRunCreate, IRunEdit, IRunMetrics, RunStatus, RunStatusFilter } from '../../models/run.model';
import { RunExportService } from '../../services/run-export.service';
import { RunService, displayRunId, formatDuration } from '../../services/run.service';
import { RunCreateModalComponent } from '../modals/run-create-modal/run-create-modal.component';

/** Runs list: regression run executions for the active project. */
@Component({
  selector: 'app-run-list-page',
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
    RunCreateModalComponent,
    ModalComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './run-list-page.component.html',
})
export class RunListPageComponent {
  private readonly service = inject(RunService);
  private readonly exportService = inject(RunExportService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly runs = this.service.runs;
  protected readonly totalCount = this.service.totalCount;
  protected readonly pageNumber = this.service.pageNumber;
  protected readonly pageSize = this.service.pageSize;
  protected readonly datasetOptions = this.service.datasetOptions;

  protected readonly createOpen = signal(false);

  /** The run being edited (drives the edit modal, prefilled); null = closed. */
  protected readonly editRun = signal<IRunEdit | null>(null);

  /** The run pending deletion (drives the confirm modal); null = closed. */
  protected readonly runToDelete = signal<IRun | null>(null);
  /** True while the delete request is in flight (spinner + disabled confirm). */
  protected readonly deleting = signal(false);
  /** Id of the run whose Excel export is generating (per-row spinner), or null. */
  protected readonly exportingId = signal<number | null>(null);

  protected readonly displayRunId = displayRunId;
  protected readonly formatDuration = formatDuration;

  protected readonly addIcon = AppIconsConstants.add;
  protected readonly searchIcon = AppIconsConstants.search;
  protected readonly viewIcon = AppIconsConstants.view;
  protected readonly startIcon = AppIconsConstants.start;
  protected readonly stopIcon = AppIconsConstants.stop;
  protected readonly editIcon = AppIconsConstants.edit;
  protected readonly deleteIcon = AppIconsConstants.delete;
  protected readonly exportIcon = AppIconsConstants.download;

  protected readonly searchControl = new FormControl<string>('', { nonNullable: true });
  protected readonly statusControl = new FormControl<RunStatusFilter>('all', { nonNullable: true });

  protected readonly statusOptions: ISelectOption[] = [
    { value: 'all', label: 'All' },
    { value: 'Completed', label: 'Completed' },
    { value: 'Running', label: 'Running' },
    { value: 'Queued', label: 'Queued' },
    { value: 'Failed', label: 'Failed' },
    { value: 'Pending', label: 'Pending' },
    { value: 'Cancelled', label: 'Cancelled' },
  ];

  protected readonly columns: IDataTableColumn[] = [
    { key: 'id', label: 'ID', widthClass: 'w-[110px]' },
    { key: 'title', label: 'Run Name' },
    { key: 'environment', label: 'Environment', align: 'center', widthClass: 'w-[110px]' },
    { key: 'runType', label: 'Run Type', widthClass: 'w-[100px]' },
    { key: 'metrics', label: 'Health (Metrics)', widthClass: 'w-[200px]' },
    { key: 'totalRequest', label: 'Total Requests', align: 'right', widthClass: 'w-[120px]' },
    { key: 'status', label: 'Status', widthClass: 'w-[130px]' },
    { key: 'durationMs', label: 'Duration', align: 'right', widthClass: 'w-[120px]' },
    { key: 'createdDate', label: 'Created', widthClass: 'w-[160px]' },
    { key: 'actions', label: 'Actions', align: 'center', widthClass: 'w-[90px]' },
  ];

  private static readonly statusTones: Record<RunStatus, StatusTone> = {
    Completed: 'success',
    Failed: 'error',
    Running: 'info',
    Queued: 'info',
    Pending: 'neutral',
    Cancelled: 'warning',
  };

  private static readonly environmentTones: Record<string, StatusTone> = {
    DEV: 'info',
    QA: 'success',
    STG: 'warning',
    UAT: 'warning',
    PROD: 'error',
    LIVE: 'error',
  };

  constructor() {
    this.searchControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((term) => this.service.setSearch(term));

    this.statusControl.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe((status) => this.service.setStatus(status));

    // Live progress while viewing the list: poll only the running runs currently on screen (by id).
    // Scoped to this page — takeUntilDestroyed stops it when navigating away (no global spam).
    interval(4000)
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.service.pollVisibleProgress());
  }

  protected asRun(row: unknown): IRun {
    return row as IRun;
  }

  protected statusTone(status: RunStatus): StatusTone {
    return RunListPageComponent.statusTones[status];
  }

  protected environmentTone(environment: string): StatusTone {
    return RunListPageComponent.environmentTones[environment.toUpperCase()] ?? 'neutral';
  }

  /** Hide the Pass/Warning/Fail words when all three are present (keeps the cell compact). */
  protected showMetricLabels(metrics: IRunMetrics): boolean {
    return !(metrics.passPercent > 0 && metrics.warningPercent > 0 && metrics.failPercent > 0);
  }

  /** Tooltip explaining the health bar (shown on hover) — what each percentage means. */
  protected metricsTooltip(metrics: IRunMetrics): string {
    return `Success: ${metrics.passPercent}%  •  Warning: ${metrics.warningPercent}%  •  Fail: ${metrics.failPercent}%`;
  }

  /** Execution-health percentages for a finished run, or null if it has no executed requests. */
  protected runMetrics(row: unknown): IRunMetrics | null {
    const run = this.asRun(row);
    const pass = run.passCount ?? 0;
    const warning = run.warningCount ?? 0;
    const fail = run.failedCount ?? 0;
    const total = pass + warning + fail;
    if (total === 0) {
      return null;
    }
    return {
      passPercent: Math.round((pass / total) * 100),
      warningPercent: Math.round((warning / total) * 100),
      failPercent: Math.round((fail / total) * 100),
    };
  }

  onPage(page: number): void {
    this.service.setPage(page);
  }

  onCreated(payload: IRunCreate): void {
    this.service.create(payload);
    this.createOpen.set(false);
  }

  /** Load the run's editable fields, then open the modal in edit mode. */
  openEdit(row: unknown): void {
    this.service
      .getForEdit(this.asRun(row).id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((run) => this.editRun.set(run));
  }

  onEdited(payload: IRunCreate): void {
    const run = this.editRun();
    if (run) {
      this.service.update(run.id, payload);
    }
    this.editRun.set(null);
  }

  startRun(row: unknown): void {
    this.service.start(this.asRun(row).id);
  }

  stopRun(row: unknown): void {
    this.service.stop(this.asRun(row).id);
  }

  /** Export the run's requests + responses to Excel (per-row spinner while generating). */
  exportRun(row: unknown): void {
    const id = this.asRun(row).id;
    if (this.exportingId() !== null) {
      return;
    }
    this.exportingId.set(id);
    this.exportService
      .export(id)
      .catch(() => undefined) // errors just release the spinner; the interceptor already toasts
      .finally(() => this.exportingId.set(null));
  }

  /** Open the delete confirmation for a run. */
  askDelete(row: unknown): void {
    this.runToDelete.set(this.asRun(row));
  }

  cancelDelete(): void {
    this.runToDelete.set(null);
  }

  /** Delete the run pending confirmation; keep the button locked/loading until the request finishes. */
  confirmDelete(): void {
    const run = this.runToDelete();
    if (!run || this.deleting()) {
      return;
    }
    this.deleting.set(true);
    this.service
      .remove(run.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.deleting.set(false);
          this.runToDelete.set(null); // close only after the delete succeeds
        },
        error: () => this.deleting.set(false), // keep the modal open so the user can retry
      });
  }

  openDetail(row: unknown): void {
    this.router.navigate([this.asRun(row).id], { relativeTo: this.route });
  }
}
