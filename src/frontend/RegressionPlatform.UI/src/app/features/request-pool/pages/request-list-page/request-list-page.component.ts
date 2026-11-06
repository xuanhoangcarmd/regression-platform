import { ChangeDetectionStrategy, Component, DestroyRef, WritableSignal, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute } from '@angular/router';
import { Observable, debounceTime, distinctUntilChanged } from 'rxjs';

import { AppIconsConstants } from '@core/constants';
import { DatasetApiService } from '@features/datasets/apis/dataset-api.service';
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

import {
  IApiRequestDetail,
  IApiRequestItem,
  IApiRequestWrite,
  RequestStatusFilter,
} from '../../models/api-request.model';
import { ApiRequestService, displayRequestId } from '../../services/api-request.service';
import { ApiRequestBulkDeleteModalComponent } from '../modals/api-request-bulk-delete-modal/api-request-bulk-delete-modal.component';
import { ApiRequestFormModalComponent } from '../modals/api-request-form-modal/api-request-form-modal.component';
import { ApiRequestImportModalComponent } from '../modals/api-request-import-modal/api-request-import-modal.component';
import { ApiRequestRemoveModalComponent } from '../modals/api-request-remove-modal/api-request-remove-modal.component';

/** Request Pool list: filter by API, search by title, status filter, paginated. */
@Component({
  selector: 'app-request-list-page',
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
    PaginatorComponent,
    ApiRequestFormModalComponent,
    ApiRequestImportModalComponent,
    ApiRequestRemoveModalComponent,
    ApiRequestBulkDeleteModalComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './request-list-page.component.html',
})
export class RequestListPageComponent {
  private readonly service = inject(ApiRequestService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly route = inject(ActivatedRoute);
  private readonly datasetApi = inject(DatasetApiService);

  protected readonly result = this.service.result;
  protected readonly loading = this.service.loading;
  protected readonly apiOptions = this.service.apiOptions;

  /** True while add/edit/delete is in flight (drives the modal confirm spinner). */
  protected readonly actionLoading = signal(false);
  /** True while fetching a request's detail before opening the edit modal (drives the table overlay). */
  protected readonly editLoading = signal(false);

  /** Name of the dataset being browsed (for the page header). */
  protected readonly datasetName = signal('');

  /** Filter dropdown = "All APIs" + the API list (the form uses apiOptions without "all"). */
  protected readonly filterApiOptions = computed<ISelectOption[]>(() => [
    { value: '', label: 'All APIs' },
    ...this.apiOptions(),
  ]);

  /** Modal state. */
  protected readonly formOpen = signal(false);
  protected readonly importOpen = signal(false);
  protected readonly bulkDeleteOpen = signal(false);
  protected readonly editingDetail = signal<IApiRequestDetail | null>(null);
  protected readonly removingRequest = signal<IApiRequestItem | null>(null);

  protected readonly apiControl = new FormControl<string>('', { nonNullable: true });
  protected readonly searchControl = new FormControl<string>('', { nonNullable: true });
  protected readonly statusControl = new FormControl<RequestStatusFilter>('all', { nonNullable: true });

  protected readonly addIcon = AppIconsConstants.add;
  protected readonly importIcon = AppIconsConstants.upload;
  protected readonly searchIcon = AppIconsConstants.search;
  protected readonly editIcon = AppIconsConstants.edit;
  protected readonly deleteIcon = AppIconsConstants.delete;

  protected readonly displayRequestId = displayRequestId;

  protected readonly statusOptions: ISelectOption[] = [
    { value: 'all', label: 'All' },
    { value: 'active', label: 'Active' },
    { value: 'inactive', label: 'Inactive' },
  ];

  protected readonly columns: IDataTableColumn[] = [
    { key: 'id', label: 'Request ID', widthClass: 'w-[120px]' },
    { key: 'apiName', label: 'API' },
    { key: 'title', label: 'Request Title' },
    { key: 'description', label: 'Description' },
    { key: 'method', label: 'Method', widthClass: 'w-[100px]' },
    { key: 'isActive', label: 'Status', widthClass: 'w-[120px]' },
    { key: 'actions', label: 'Actions', align: 'center', widthClass: 'w-[100px]' },
  ];

  constructor() {
    // Point the shared request-list state at this route's dataset, and load its name for the header.
    const datasetId = Number(this.route.snapshot.paramMap.get('datasetId'));
    this.service.setDataset(datasetId);
    this.datasetApi
      .getById(datasetId)
      .pipe(takeUntilDestroyed())
      .subscribe((dataset) => this.datasetName.set(dataset.name));

    this.searchControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((term) => this.service.setSearch(term));

    this.statusControl.valueChanges.pipe(takeUntilDestroyed()).subscribe((status) => this.service.setStatus(status));

    this.apiControl.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe((value) => this.service.setApiId(value ? Number(value) : null));
  }

  protected asRequest(row: unknown): IApiRequestItem {
    return row as IApiRequestItem;
  }

  onPage(page: number): void {
    this.service.setPage(page);
  }

  openAdd(): void {
    this.editingDetail.set(null);
    this.formOpen.set(true);
  }

  openImport(): void {
    this.importOpen.set(true);
  }

  openEdit(row: unknown): void {
    const startedAt = Date.now();
    this.editLoading.set(true);
    this.service
      .getById(this.asRequest(row).id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (detail) =>
          this.finishAction(startedAt, this.editLoading, () => {
            this.editingDetail.set(detail);
            this.formOpen.set(true);
          }),
        error: () => this.finishAction(startedAt, this.editLoading),
      });
  }

  onSubmitted(payload: IApiRequestWrite): void {
    const editing = this.editingDetail();
    const startedAt = Date.now();
    this.actionLoading.set(true);
    const request$: Observable<unknown> = editing
      ? this.service.update(editing.id, payload)
      : this.service.create(payload);
    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.finishAction(startedAt, this.actionLoading, () => this.formOpen.set(false)),
      error: () => this.finishAction(startedAt, this.actionLoading),
    });
  }

  openRemove(row: unknown): void {
    this.removingRequest.set(this.asRequest(row));
  }

  /** Delete all requests (+ responses) of the chosen API in this dataset, then close the modal. */
  onBulkDeleteConfirmed(apiId: number): void {
    const startedAt = Date.now();
    this.actionLoading.set(true);
    this.service
      .removeByApi(apiId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.finishAction(startedAt, this.actionLoading, () => this.bulkDeleteOpen.set(false)),
        error: () => this.finishAction(startedAt, this.actionLoading),
      });
  }

  onRemoveConfirmed(request: IApiRequestItem): void {
    const startedAt = Date.now();
    this.actionLoading.set(true);
    this.service
      .remove(request.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.finishAction(startedAt, this.actionLoading, () => this.removingRequest.set(null)),
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
    const remaining = RequestListPageComponent.MIN_LOADING_MS - (Date.now() - startedAt);
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
