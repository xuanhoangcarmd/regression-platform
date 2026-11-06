import { ChangeDetectionStrategy, Component, DestroyRef, WritableSignal, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, debounceTime, distinctUntilChanged } from 'rxjs';

import { AppIconsConstants } from '@core/constants';
import {
  ButtonComponent,
  CellTemplateDirective,
  DataTableComponent,
  IDataTableColumn,
  ISelectOption,
  InputComponent,
  SelectComponent,
  StatusBadgeComponent,
} from '@shared/components';

import { DatasetStatusFilter, IDataset, IDatasetItem, IDatasetUpdate } from '../../models/dataset.model';
import { DatasetService } from '../../services/dataset.service';
import { DatasetFormModalComponent } from '../modals/dataset-form-modal/dataset-form-modal.component';
import { DatasetRemoveModalComponent } from '../modals/dataset-remove-modal/dataset-remove-modal.component';

/** Dataset List: a project's datasets (named request collections), with request + API counts. */
@Component({
  selector: 'app-dataset-list-page',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatIconModule,
    ButtonComponent,
    InputComponent,
    SelectComponent,
    DataTableComponent,
    CellTemplateDirective,
    StatusBadgeComponent,
    DatasetFormModalComponent,
    DatasetRemoveModalComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dataset-list-page.component.html',
})
export class DatasetListPageComponent {
  private readonly service = inject(DatasetService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly datasets = this.service.datasets;
  protected readonly loading = this.service.loading;

  protected readonly formOpen = signal(false);
  protected readonly editingDataset = signal<IDataset | null>(null);
  protected readonly removingDataset = signal<IDatasetItem | null>(null);

  /** True while add/edit/delete is in flight (drives the modal confirm spinner). */
  protected readonly actionLoading = signal(false);
  /** True while fetching a dataset's detail before opening the edit modal (drives the table overlay). */
  protected readonly editLoading = signal(false);

  protected readonly searchControl = new FormControl<string>('', { nonNullable: true });
  protected readonly statusControl = new FormControl<DatasetStatusFilter>('all', { nonNullable: true });

  protected readonly addIcon = AppIconsConstants.add;
  protected readonly pinIcon = AppIconsConstants.pin;
  protected readonly searchIcon = AppIconsConstants.search;
  protected readonly viewIcon = AppIconsConstants.view;
  protected readonly editIcon = AppIconsConstants.edit;
  protected readonly deleteIcon = AppIconsConstants.delete;

  protected readonly statusOptions: ISelectOption[] = [
    { value: 'all', label: 'All' },
    { value: 'active', label: 'Active' },
    { value: 'inactive', label: 'Inactive' },
  ];

  protected readonly columns: IDataTableColumn[] = [
    { key: 'pinned', label: '', align: 'center', widthClass: 'w-[48px]' },
    { key: 'name', label: 'Dataset' },
    { key: 'requestCount', label: 'Requests', align: 'right', widthClass: 'w-[120px]' },
    { key: 'apiCount', label: 'APIs', align: 'right', widthClass: 'w-[100px]' },
    { key: 'isActive', label: 'Status', widthClass: 'w-[120px]' },
    { key: 'actions', label: 'Actions', align: 'center', widthClass: 'w-[120px]' },
  ];

  constructor() {
    this.searchControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((term) => this.service.setSearch(term));

    this.statusControl.valueChanges.pipe(takeUntilDestroyed()).subscribe((status) => this.service.setStatus(status));
  }

  protected asDataset(row: unknown): IDatasetItem {
    return row as IDatasetItem;
  }

  protected isPinned(row: unknown): boolean {
    return this.service.isPinned(this.asDataset(row).id);
  }

  protected togglePin(row: unknown): void {
    this.service.togglePin(this.asDataset(row).id);
  }

  openRequests(row: unknown): void {
    this.router.navigate([this.asDataset(row).id, 'requests'], { relativeTo: this.route });
  }

  openAdd(): void {
    this.editingDataset.set(null);
    this.formOpen.set(true);
  }

  openEdit(row: unknown): void {
    const startedAt = Date.now();
    this.editLoading.set(true);
    this.service
      .getById(this.asDataset(row).id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (detail) =>
          this.finishAction(startedAt, this.editLoading, () => {
            this.editingDataset.set(detail);
            this.formOpen.set(true);
          }),
        error: () => this.finishAction(startedAt, this.editLoading),
      });
  }

  onSubmitted(payload: IDatasetUpdate): void {
    const editing = this.editingDataset();
    const startedAt = Date.now();
    this.actionLoading.set(true);
    const request$: Observable<unknown> = editing
      ? this.service.update(editing.id, payload)
      : this.service.create({ name: payload.name, description: payload.description });
    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.finishAction(startedAt, this.actionLoading, () => this.formOpen.set(false)),
      error: () => this.finishAction(startedAt, this.actionLoading),
    });
  }

  openRemove(row: unknown): void {
    this.removingDataset.set(this.asDataset(row));
  }

  onRemoveConfirmed(dataset: IDatasetItem): void {
    const startedAt = Date.now();
    this.actionLoading.set(true);
    this.service
      .remove(dataset.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.finishAction(startedAt, this.actionLoading, () => this.removingDataset.set(null)),
        error: () => this.finishAction(startedAt, this.actionLoading),
      });
  }

  /**
   * Minimum time (ms) a loading state stays visible — on dev the API is local and near-instant;
   * without a floor the spinner flashes and users think nothing happened.
   */
  private static readonly MIN_LOADING_MS = 500;

  /** Clear the given loading flag after the minimum visible delay, then run `onDone`. */
  private finishAction(startedAt: number, flag: WritableSignal<boolean>, onDone?: () => void): void {
    const remaining = DatasetListPageComponent.MIN_LOADING_MS - (Date.now() - startedAt);
    const run = () => {
      flag.set(false);
      onDone?.();
    };
    if (remaining > 0) {
      setTimeout(run, remaining);
    } else {
      run();
    }
  }
}
