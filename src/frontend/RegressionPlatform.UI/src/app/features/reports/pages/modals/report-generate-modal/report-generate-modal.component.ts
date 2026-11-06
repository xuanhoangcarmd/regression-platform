import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { catchError, map, of } from 'rxjs';

import { ISelectOption, ModalComponent, SelectComponent } from '@shared/components';

import { CompareApiService } from '@features/compare/apis/compare-api.service';
import { ICompareJob } from '@features/compare/models/compare.model';
import { displayCompareId } from '@features/compare/services/compare.service';

/** Pick a Completed compare to generate (or regenerate) its report. */
@Component({
  selector: 'app-report-generate-modal',
  standalone: true,
  imports: [ReactiveFormsModule, ModalComponent, SelectComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './report-generate-modal.component.html',
})
export class ReportGenerateModalComponent {
  readonly generating = input<boolean>(false);
  readonly closed = output<void>();
  readonly submitted = output<number>();

  private readonly compareApi = inject(CompareApiService);

  protected readonly displayCompareId = displayCompareId;

  /** Only Completed compares can be reported on. */
  private readonly compares = toSignal(
    this.compareApi.getCompares().pipe(
      map((compares) => compares.filter((compare) => compare.status === 'Completed')),
      catchError(() => of<ICompareJob[]>([])),
    ),
    { initialValue: [] as ICompareJob[] },
  );

  protected readonly compareOptions = computed<ISelectOption[]>(() => [
    { value: '', label: '-- Select Compare --' },
    ...this.compares().map((compare) => ({
      value: String(compare.id),
      label: `${displayCompareId(compare.id)} — ${compare.runATitle} vs ${compare.runBTitle}`,
    })),
  ]);

  protected readonly compareControl = new FormControl<string>('', { nonNullable: true });

  protected selectedCompare(): ICompareJob | undefined {
    return this.compares().find((compare) => String(compare.id) === this.compareControl.value);
  }

  protected isInvalid(): boolean {
    return !this.compareControl.value || this.generating();
  }

  protected save(): void {
    if (this.isInvalid()) {
      return;
    }
    this.submitted.emit(Number(this.compareControl.value));
  }
}
