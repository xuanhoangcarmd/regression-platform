import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { DatasetApiService } from '@features/datasets/apis/dataset-api.service';
import { ISelectOption, InputComponent, ModalComponent, SelectComponent, TextareaComponent, ToggleComponent } from '@shared/components';
import { controlErrorMessage } from '@shared/utils';

import { IRunCreate, IRunEdit } from '../../../models/run.model';

/** Create or edit a regression run: pick a dataset, then the APIs (within it) to run. */
@Component({
  selector: 'app-run-create-modal',
  standalone: true,
  imports: [ReactiveFormsModule, ModalComponent, InputComponent, SelectComponent, TextareaComponent, ToggleComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './run-create-modal.component.html',
})
export class RunCreateModalComponent implements OnInit {
  /** Active datasets to choose from (value = id as string, label = name). */
  readonly datasetOptions = input<ISelectOption[]>([]);
  /** When set, the modal is in edit mode: fields are prefilled from this run. */
  readonly editRun = input<IRunEdit | null>(null);
  readonly closed = output<void>();
  readonly submitted = output<IRunCreate>();

  /** True in edit mode (drives the title + confirm label). */
  protected readonly isEdit = computed(() => this.editRun() !== null);

  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly datasetApi = inject(DatasetApiService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly controlErrorMessage = controlErrorMessage;

  /** APIs in the selected dataset (value = id as string, label = name). */
  protected readonly apiOptions = signal<ISelectOption[]>([]);
  /** Selected API ids when "Select specific APIs" is active. */
  protected readonly selectedApiIds = signal<Set<number>>(new Set());

  protected readonly runTypeOptions: ISelectOption[] = [
    { value: 'Sprint', label: 'Sprint' },
    { value: 'Hotfix', label: 'Hotfix' },
    { value: 'Smoke', label: 'Smoke' },
    { value: 'Regression', label: 'Regression' },
  ];

  protected readonly environmentOptions: ISelectOption[] = [
    { value: 'DEV', label: 'Development' },
    { value: 'STG', label: 'Staging' },
    { value: 'PROD', label: 'Production' },
  ];

  protected readonly form = this.formBuilder.group({
    datasetId: ['', [Validators.required]],
    title: ['', [Validators.required, Validators.maxLength(300)]],
    runType: ['Sprint'],
    environment: ['STG'],
    description: [''],
    // Optional cap; empty = run all requests of each API. Positive integer when set.
    requestLimit: ['', [Validators.pattern(/^[1-9]\d*$/)]],
    apiScope: ['all'],
    // Parallel execution (off = sequential 1×1). When on, the two counts below apply.
    parallel: [false],
    maxParallelApis: ['2', [Validators.pattern(/^[1-9]\d*$/)]],
    maxParallelRequestsPerApi: ['5', [Validators.pattern(/^[1-9]\d*$/)]],
  });

  /** Max allowed product of the two parallelism knobs (protects the target server). */
  protected readonly maxParallelProduct = 100;

  /** Effective concurrency = APIs × requests/API. Shown live and validated against the cap. */
  protected parallelProduct(): number {
    const v = this.form.getRawValue();
    return (Number(v.maxParallelApis) || 0) * (Number(v.maxParallelRequestsPerApi) || 0);
  }

  /** True when parallel is on but the product exceeds the cap (blocks submit). */
  protected productExceeded(): boolean {
    return this.form.controls.parallel.value && this.parallelProduct() > this.maxParallelProduct;
  }

  constructor() {
    // When the dataset changes, load its APIs and reset the API scope to "all".
    this.form.controls.datasetId.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((value) => {
      this.selectedApiIds.set(new Set());
      this.form.controls.apiScope.setValue('all');
      this.apiOptions.set([]);
      if (!value) {
        return;
      }
      this.datasetApi
        .getApis(Number(value))
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe((apis) => this.apiOptions.set(apis.map((api) => ({ value: String(api.id), label: api.name }))));
    });
  }

  ngOnInit(): void {
    const edit = this.editRun();
    if (!edit) {
      return;
    }
    // Prefill without emitting (so the datasetId subscription doesn't reset apiScope/selection), then
    // set the API selection and load the dataset's APIs ourselves so ordering is deterministic.
    this.form.patchValue(
      {
        datasetId: String(edit.datasetId),
        title: edit.title,
        runType: edit.runType,
        environment: edit.environment,
        description: edit.description ?? '',
        requestLimit: edit.requestLimitPerApi != null ? String(edit.requestLimitPerApi) : '',
        apiScope: edit.apiIds.length > 0 ? 'specific' : 'all',
        parallel: edit.maxParallelApis > 1 || edit.maxParallelRequestsPerApi > 1,
        maxParallelApis: String(edit.maxParallelApis),
        maxParallelRequestsPerApi: String(edit.maxParallelRequestsPerApi),
      },
      { emitEvent: false },
    );
    this.selectedApiIds.set(new Set(edit.apiIds));
    this.datasetApi
      .getApis(edit.datasetId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((apis) => this.apiOptions.set(apis.map((api) => ({ value: String(api.id), label: api.name }))));
  }

  protected isApiSelected(id: number): boolean {
    return this.selectedApiIds().has(id);
  }

  protected toggleApi(id: number): void {
    this.selectedApiIds.update((current) => {
      const next = new Set(current);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  }

  protected apiId(option: ISelectOption): number {
    return Number(option.value);
  }

  /** Disable confirm when invalid, "specific" chosen with nothing selected, or parallel product too big. */
  protected isInvalid(): boolean {
    return (
      this.form.invalid ||
      (this.form.controls.apiScope.value === 'specific' && this.selectedApiIds().size === 0) ||
      this.productExceeded()
    );
  }

  protected save(): void {
    if (this.isInvalid()) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const limit = value.requestLimit.trim();
    // Off = sequential (1×1); on = the chosen knobs (guarded ≥ 1).
    const parallel = value.parallel;
    const maxParallelApis = parallel ? Math.max(1, Number(value.maxParallelApis) || 1) : 1;
    const maxParallelRequestsPerApi = parallel ? Math.max(1, Number(value.maxParallelRequestsPerApi) || 1) : 1;
    this.submitted.emit({
      datasetId: Number(value.datasetId),
      title: value.title.trim(),
      runType: value.runType,
      environment: value.environment,
      description: value.description.trim() || null,
      requestLimitPerApi: limit ? Number(limit) : null,
      apiIds: value.apiScope === 'specific' ? [...this.selectedApiIds()] : [],
      maxParallelApis,
      maxParallelRequestsPerApi,
    });
  }
}
