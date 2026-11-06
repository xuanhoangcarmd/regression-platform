import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { catchError, distinctUntilChanged, forkJoin, map, of, startWith, switchMap } from 'rxjs';

import { ApiDefinitionApiService } from '@features/api-definitions/apis/api-definition-api.service';
import { ExpectedResponseApiService } from '@features/regression-runs/apis/expected-response-api.service';
import { RunApiService } from '@features/regression-runs/apis/run-api.service';
import { CompareBaselineMode } from '@features/regression-runs/models/expected-response.model';
import { IRun } from '@features/regression-runs/models/run.model';
import { displayRunId } from '@features/regression-runs/services/run.service';
import { ISelectOption, ModalComponent, SelectComponent } from '@shared/components';

import { ICompareCreate } from '../../../models/compare.model';

/** Create a compare of two Completed runs (Run A → Run B). */
@Component({
  selector: 'app-compare-create-modal',
  standalone: true,
  imports: [DatePipe, DecimalPipe, ReactiveFormsModule, ModalComponent, SelectComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './compare-create-modal.component.html',
})
export class CompareCreateModalComponent {
  readonly loading = input<boolean>(false);
  readonly closed = output<void>();
  readonly submitted = output<ICompareCreate>();

  private readonly runApi = inject(RunApiService);
  private readonly apiDefinitionApi = inject(ApiDefinitionApiService);
  private readonly expectedApi = inject(ExpectedResponseApiService);
  private readonly formBuilder = inject(NonNullableFormBuilder);

  protected readonly displayRunId = displayRunId;

  /** Completed runs are the only ones eligible to compare. */
  private readonly runs = toSignal(
    this.runApi.getRuns().pipe(
      map((runs) => runs.filter((run) => run.status === 'Completed')),
      catchError(() => of<IRun[]>([])),
    ),
    { initialValue: [] as IRun[] },
  );

  /** Run dropdown options (with a placeholder); value = id as string. */
  protected readonly runOptions = computed<ISelectOption[]>(() => [
    { value: '', label: '-- Select Run --' },
    ...this.runs().map((run) => ({ value: String(run.id), label: `${displayRunId(run.id)} - ${run.title}` })),
  ]);

  /** Project APIs for the "specific APIs" scope. */
  protected readonly apiOptions = toSignal(
    this.apiDefinitionApi.getApis({ pageNumber: 1, pageSize: 100, isActive: true }).pipe(
      map((page) => page.items.map((api) => ({ value: String(api.id), label: api.name }) as ISelectOption)),
      catchError(() => of<ISelectOption[]>([])),
    ),
    { initialValue: [] as ISelectOption[] },
  );

  protected readonly selectedApiIds = signal<Set<number>>(new Set());

  protected readonly form = this.formBuilder.group({
    runAId: ['', [Validators.required]],
    runBId: ['', [Validators.required]],
    apiScope: ['all'],
    baselineMode: ['Expected' as CompareBaselineMode],
    /** Default on (the original behaviour); off = key order and array order count as differences. */
    ignoreOrder: [true],
  });

  /** Edited (expected) responses across both selected runs (null = no run selected / loading). */
  protected readonly editedCount = toSignal(
    this.form.valueChanges.pipe(
      startWith(this.form.getRawValue()),
      map((value) => [value.runAId, value.runBId].filter((id): id is string => !!id)),
      distinctUntilChanged((a, b) => a.join() === b.join()),
      switchMap((ids) =>
        ids.length
          ? forkJoin(ids.map((id) => this.expectedApi.countEdited(Number(id)).pipe(catchError(() => of(0))))).pipe(
              map((counts) => counts.reduce((sum, count) => sum + count, 0)),
            )
          : of(null),
      ),
    ),
    { initialValue: null as number | null },
  );

  constructor() {
    // Clear specific-API selection when switching back to "all".
    this.form.controls.apiScope.valueChanges.pipe(takeUntilDestroyed()).subscribe((scope) => {
      if (scope === 'all') {
        this.selectedApiIds.set(new Set());
      }
    });
  }

  /** The selected run object for a control value (for the meta line), or undefined. */
  protected selectedRun(idValue: string): IRun | undefined {
    return this.runs().find((run) => String(run.id) === idValue);
  }

  protected apiId(option: ISelectOption): number {
    return Number(option.value);
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

  /** Disable confirm when runs are missing/equal, or "specific" chosen with nothing selected. */
  protected isInvalid(): boolean {
    const { runAId, runBId, apiScope } = this.form.getRawValue();
    if (!runAId || !runBId || runAId === runBId) {
      return true;
    }
    return apiScope === 'specific' && this.selectedApiIds().size === 0;
  }

  protected save(): void {
    if (this.isInvalid()) {
      return;
    }
    const value = this.form.getRawValue();
    this.submitted.emit({
      runAId: Number(value.runAId),
      runBId: Number(value.runBId),
      apiIds: value.apiScope === 'specific' ? [...this.selectedApiIds()] : [],
      baselineMode: value.baselineMode,
      ignoreOrder: value.ignoreOrder,
    });
  }
}
