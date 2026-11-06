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

import {
  CompareStatus,
  CompareStatusFilter,
  ICompareCreate,
  ICompareJob,
  ICompareMetrics,
} from '../../models/compare.model';
import { CompareService, displayCompareId } from '../../services/compare.service';
import { CompareCreateModalComponent } from '../modals/compare-create-modal/compare-create-modal.component';

/** Compare History: comparisons of two runs for the active project. */
@Component({
  selector: 'app-compare-list-page',
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
    CompareCreateModalComponent,
    ModalComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './compare-list-page.component.html',
})
export class CompareListPageComponent {
  private readonly service = inject(CompareService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly compares = this.service.compares;
  protected readonly totalCount = this.service.totalCount;
  protected readonly pageNumber = this.service.pageNumber;
  protected readonly pageSize = this.service.pageSize;
  protected readonly loading = this.service.loading;

  protected readonly createOpen = signal(false);
  /** True while creating a compare (drives the modal confirm spinner). */
  protected readonly createLoading = signal(false);
  /** Id of the compare currently being started (per-row spinner), or null. */
  protected readonly startingId = signal<number | null>(null);
  /** The compare pending deletion (drives the confirm modal); null = closed. */
  protected readonly compareToDelete = signal<ICompareJob | null>(null);
  /** True while the delete request is in flight (spinner + disabled confirm). */
  protected readonly deleting = signal(false);

  protected readonly displayCompareId = displayCompareId;

  protected readonly addIcon = AppIconsConstants.add;
  protected readonly searchIcon = AppIconsConstants.search;
  protected readonly viewIcon = AppIconsConstants.view;
  protected readonly startIcon = AppIconsConstants.start;
  protected readonly stopIcon = AppIconsConstants.stop;
  protected readonly deleteIcon = AppIconsConstants.delete;
  protected readonly arrowIcon = AppIconsConstants.arrowForward;

  /** Id of the compare currently being stopped (per-row spinner), or null. */
  protected readonly stoppingId = signal<number | null>(null);

  protected readonly searchControl = new FormControl<string>('', { nonNullable: true });
  protected readonly statusControl = new FormControl<CompareStatusFilter>('all', { nonNullable: true });

  protected readonly statusOptions: ISelectOption[] = [
    { value: 'all', label: 'All' },
    { value: 'Completed', label: 'Completed' },
    { value: 'Running', label: 'Running' },
    { value: 'Failed', label: 'Failed' },
    { value: 'Pending', label: 'Pending' },
    { value: 'Cancelled', label: 'Cancelled' },
  ];

  protected readonly columns: IDataTableColumn[] = [
    { key: 'id', label: 'ID', widthClass: 'w-[110px]' },
    { key: 'runs', label: 'Runs (A → B)' },
    { key: 'status', label: 'Status', widthClass: 'w-[130px]' },
    { key: 'result', label: 'Result', widthClass: 'w-[220px]' },
    { key: 'totalRequest', label: 'Total', align: 'right', widthClass: 'w-[90px]' },
    { key: 'createdDate', label: 'Created', widthClass: 'w-[160px]' },
    { key: 'actions', label: 'Actions', align: 'center', widthClass: 'w-[80px]' },
  ];

  private static readonly statusTones: Record<CompareStatus, StatusTone> = {
    Completed: 'success',
    Failed: 'error',
    Running: 'info',
    Pending: 'neutral',
    Cancelled: 'warning',
  };

  constructor() {
    this.searchControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((term) => this.service.setSearch(term));

    this.statusControl.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe((status) => this.service.setStatus(status));

    // Live updates while viewing the list: poll only while a compare is executing.
    // Scoped to this page — takeUntilDestroyed stops it when navigating away.
    interval(4000)
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.service.pollActive());
  }

  protected asCompare(row: unknown): ICompareJob {
    return row as ICompareJob;
  }

  protected statusTone(status: CompareStatus): StatusTone {
    return CompareListPageComponent.statusTones[status];
  }

  /** Per-request result breakdown as percentages, or null when the compare ran nothing. */
  protected compareMetrics(job: ICompareJob): ICompareMetrics | null {
    const unchanged = job.unchangedCount ?? 0;
    const changed = job.changedCount ?? 0;
    const added = job.addedCount ?? 0;
    const removed = job.removedCount ?? 0;
    const failed = job.failedCount ?? 0;
    const total = unchanged + changed + added + removed + failed;
    if (total === 0) {
      return null;
    }
    const percent = (count: number): number => Math.round((count / total) * 100);
    return {
      unchangedPercent: percent(unchanged),
      changedPercent: percent(changed),
      addedPercent: percent(added),
      removedPercent: percent(removed),
      failedPercent: percent(failed),
    };
  }

  /** Show the category words next to each % only when few (≤ 2) are present, to keep the cell compact. */
  protected showCompareLabels(m: ICompareMetrics): boolean {
    const present = [
      m.unchangedPercent,
      m.changedPercent,
      m.addedPercent,
      m.removedPercent,
      m.failedPercent,
    ].filter((value) => value > 0).length;
    return present <= 2;
  }

  /** Tooltip explaining the result bar (shown on hover) — what each percentage means. */
  protected metricsTooltip(m: ICompareMetrics): string {
    return (
      `Unchanged: ${m.unchangedPercent}%  •  Changed: ${m.changedPercent}%  •  ` +
      `Added: ${m.addedPercent}%  •  Removed: ${m.removedPercent}%  •  Failed: ${m.failedPercent}%`
    );
  }

  onPage(page: number): void {
    this.service.setPage(page);
  }

  onCreated(payload: ICompareCreate): void {
    const startedAt = Date.now();
    this.createLoading.set(true);
    this.service
      .create(payload)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.finishAfterMinDelay(startedAt, () => {
          this.createLoading.set(false);
          this.createOpen.set(false);
        }),
        error: () => this.finishAfterMinDelay(startedAt, () => this.createLoading.set(false)),
      });
  }

  startCompare(row: unknown): void {
    const id = this.asCompare(row).id;
    const startedAt = Date.now();
    this.startingId.set(id);
    this.service
      .start(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.finishAfterMinDelay(startedAt, () => this.startingId.set(null)),
        error: () => this.finishAfterMinDelay(startedAt, () => this.startingId.set(null)),
      });
  }

  /**
   * Minimum time (ms) a loading state stays visible — on dev the API is local and near-instant;
   * without a floor the spinner flashes and users think nothing happened.
   */
  private static readonly MIN_LOADING_MS = 500;

  private finishAfterMinDelay(startedAt: number, done: () => void): void {
    const remaining = CompareListPageComponent.MIN_LOADING_MS - (Date.now() - startedAt);
    if (remaining > 0) {
      setTimeout(done, remaining);
    } else {
      done();
    }
  }

  openDetail(row: unknown): void {
    this.router.navigate([this.asCompare(row).id], { relativeTo: this.route });
  }

  /** Stop a running compare (marks it Cancelled); a per-row spinner shows until it settles. */
  stopCompare(row: unknown): void {
    const id = this.asCompare(row).id;
    if (this.stoppingId() !== null) {
      return;
    }
    this.stoppingId.set(id);
    this.service
      .stop(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.stoppingId.set(null),
        error: () => this.stoppingId.set(null),
      });
  }

  /** Open the delete confirmation for a compare. */
  askDelete(row: unknown): void {
    this.compareToDelete.set(this.asCompare(row));
  }

  cancelDelete(): void {
    this.compareToDelete.set(null);
  }

  /** Delete the compare pending confirmation; keep the confirm locked/loading until it finishes. */
  confirmDelete(): void {
    const compare = this.compareToDelete();
    if (!compare || this.deleting()) {
      return;
    }
    this.deleting.set(true);
    this.service
      .remove(compare.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.deleting.set(false);
          this.compareToDelete.set(null); // close only after the delete succeeds
        },
        error: () => this.deleting.set(false), // keep the modal open so the user can retry
      });
  }
}
