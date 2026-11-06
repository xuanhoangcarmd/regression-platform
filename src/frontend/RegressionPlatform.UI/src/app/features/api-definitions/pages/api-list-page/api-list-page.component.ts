import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { debounceTime, distinctUntilChanged } from 'rxjs';

import { AppIconsConstants } from '@core/constants';
import {
  ButtonComponent,
  CellTemplateDirective,
  DataTableComponent,
  IDataTableColumn,
  ISelectOption,
  InputComponent,
  PaginatorComponent,
  SelectComponent,
  StatusBadgeComponent,
} from '@shared/components';

import { ApiDefinitionCreate, ApiStatusFilter, IApiDefinition } from '../../models/api-definition.model';
import { ApiDefinitionService, ApiDefinitionSortField, displayApiId } from '../../services/api-definition.service';
import { ApiAddModalComponent } from '../modals/api-add-modal/api-add-modal.component';
import { ApiEditModalComponent } from '../modals/api-edit-modal/api-edit-modal.component';
import { ApiImportModalComponent } from '../modals/api-import-modal/api-import-modal.component';
import { ApiRemoveModalComponent } from '../modals/api-remove-modal/api-remove-modal.component';

/** APIs list page: search, status filter, sortable table, paginated. */
@Component({
  selector: 'app-api-list-page',
  standalone: true,
  imports: [
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
    ApiAddModalComponent,
    ApiImportModalComponent,
    ApiEditModalComponent,
    ApiRemoveModalComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './api-list-page.component.html',
})
export class ApiListPageComponent {
  private readonly service = inject(ApiDefinitionService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly result = this.service.result;
  protected readonly loading = this.service.loading;
  protected readonly sortBy = this.service.sortBy;
  protected readonly sortDirection = this.service.sortDirection;

  /** True while an add / edit / delete action is in flight (drives the modal confirm spinner). */
  protected readonly actionLoading = signal(false);

  protected readonly searchControl = new FormControl<string>('', { nonNullable: true });
  protected readonly statusControl = new FormControl<ApiStatusFilter>('all', { nonNullable: true });

  /** Modal state: add visibility + the row being edited / removed. */
  protected readonly addingApi = signal(false);
  protected readonly importingApi = signal(false);
  protected readonly editingApi = signal<IApiDefinition | null>(null);
  protected readonly removingApi = signal<IApiDefinition | null>(null);

  protected readonly addIcon = AppIconsConstants.add;
  protected readonly importIcon = AppIconsConstants.download;
  protected readonly searchIcon = AppIconsConstants.search;
  protected readonly editIcon = AppIconsConstants.edit;
  protected readonly deleteIcon = AppIconsConstants.delete;

  protected readonly displayApiId = displayApiId;

  protected readonly statusOptions: ISelectOption[] = [
    { value: 'all', label: 'All' },
    { value: 'active', label: 'Active' },
    { value: 'inactive', label: 'Inactive' },
  ];

  protected readonly columns: IDataTableColumn[] = [
    { key: 'id', label: 'API ID', sortable: true, widthClass: 'w-24' },
    { key: 'name', label: 'API Name', sortable: true },
    { key: 'method', label: 'Method', sortable: true, widthClass: 'w-24' },
    { key: 'endpoint', label: 'Endpoint', sortable: true },
    { key: 'requestCount', label: 'Requests', sortable: true, align: 'right', widthClass: 'w-28' },
    { key: 'isActive', label: 'Status', sortable: true, widthClass: 'w-28' },
    { key: 'actions', label: 'Actions', align: 'center', widthClass: 'w-24' },
  ];

  constructor() {
    this.searchControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((term) => this.service.setSearch(term));

    this.statusControl.valueChanges.pipe(takeUntilDestroyed()).subscribe((status) => this.service.setStatus(status));
  }

  /** Narrows the table's untyped row to our model for use in cell templates. */
  protected asApi(row: unknown): IApiDefinition {
    return row as IApiDefinition;
  }

  onSort(key: string): void {
    this.service.toggleSort(key as ApiDefinitionSortField);
  }

  onPage(page: number): void {
    this.service.setPage(page);
  }

  onCreated(data: ApiDefinitionCreate): void {
    const startedAt = Date.now();
    this.actionLoading.set(true);
    this.service
      .add(data)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.finishAction(startedAt, () => this.addingApi.set(false)),
        error: () => this.finishAction(startedAt),
      });
  }

  openEdit(row: unknown): void {
    this.editingApi.set(this.asApi(row));
  }

  openRemove(row: unknown): void {
    this.removingApi.set(this.asApi(row));
  }

  onEditSaved(updated: IApiDefinition): void {
    const startedAt = Date.now();
    this.actionLoading.set(true);
    this.service
      .update(updated)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.finishAction(startedAt, () => this.editingApi.set(null)),
        error: () => this.finishAction(startedAt),
      });
  }

  onRemoveConfirmed(api: IApiDefinition): void {
    const startedAt = Date.now();
    this.actionLoading.set(true);
    this.service
      .remove(api.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.finishAction(startedAt, () => this.removingApi.set(null)),
        error: () => this.finishAction(startedAt),
      });
  }

  /**
   * Minimum time (ms) the action spinner stays visible — on dev the API is local and near-instant;
   * without a floor the spinner flashes and users think nothing happened.
   */
  private static readonly MIN_LOADING_MS = 500;

  /** Clear actionLoading after the minimum visible delay, then run `onDone` (e.g. close the modal). */
  private finishAction(startedAt: number, onDone?: () => void): void {
    const remaining = ApiListPageComponent.MIN_LOADING_MS - (Date.now() - startedAt);
    const run = () => {
      this.actionLoading.set(false);
      onDone?.();
    };
    if (remaining > 0) {
      setTimeout(run, remaining);
    } else {
      run();
    }
  }
}
