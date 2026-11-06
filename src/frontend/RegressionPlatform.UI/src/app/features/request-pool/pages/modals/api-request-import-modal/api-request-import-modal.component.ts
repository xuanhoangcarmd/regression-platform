import { ChangeDetectionStrategy, Component, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';

import { IProblemDetails } from '@core/models';
import { ISelectOption, ModalComponent, SelectComponent } from '@shared/components';
import { controlErrorMessage } from '@shared/utils';

import { IRequestImportResult } from '../../../models/api-request.model';
import { ApiRequestService } from '../../../services/api-request.service';

/** Bulk-import dialog: pick an API + an Excel file, upload, and show the result counts. */
@Component({
  selector: 'app-api-request-import-modal',
  standalone: true,
  imports: [ReactiveFormsModule, ModalComponent, SelectComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './api-request-import-modal.component.html',
})
export class ApiRequestImportModalComponent {
  readonly apiOptions = input<ISelectOption[]>([]);
  readonly closed = output<void>();
  readonly imported = output<IRequestImportResult>();

  private readonly service = inject(ApiRequestService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly controlErrorMessage = controlErrorMessage;

  protected readonly apiControl = new FormControl<string>('', { nonNullable: true, validators: [Validators.required] });
  protected readonly file = signal<File | null>(null);

  protected readonly loading = signal(false);
  protected readonly result = signal<IRequestImportResult | null>(null);
  protected readonly errorMessage = signal<string | null>(null);

  /** Example columns shown so users understand the file format (Title + body fields). */
  protected readonly exampleColumns = ['Title', 'Description', 'VIN', 'Mileage', 'StoreNumber', 'ReportId'];
  protected readonly exampleRows = [
    ['Honda Civic 2018', 'Smoke test — base trim', '1HGBH41JXMN109186', '45000', 'S-102', 'RPT-001'],
    ['Toyota Corolla 2019', '', '2T1BURHE0JC014567', '32000', 'S-118', 'RPT-002'],
  ];

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.file.set(input.files?.[0] ?? null);
    this.result.set(null);
    this.errorMessage.set(null);
  }

  protected submit(): void {
    const file = this.file();
    if (this.apiControl.invalid || file === null) {
      this.apiControl.markAsTouched();
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    this.service
      .importFromExcel(Number(this.apiControl.value), file)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          this.result.set(result);
          this.imported.emit(result);
        },
        // The errorInterceptor normalizes HTTP errors into IProblemDetails before they reach here.
        error: (error: IProblemDetails) => {
          this.loading.set(false);
          this.errorMessage.set(
            ApiRequestImportModalComponent.extractMessage(error) ??
              'Import failed. Check the selected API and the Excel file format (.xlsx).',
          );
        },
      });
  }

  /** Pulls the most specific message from a normalized ProblemDetails (detail → validation errors → title). */
  private static extractMessage(problem: IProblemDetails | null | undefined): string | null {
    if (!problem) {
      return null;
    }
    if (typeof problem.detail === 'string' && problem.detail.trim()) {
      return problem.detail.trim();
    }
    if (problem.errors) {
      const messages = Object.values(problem.errors).flat();
      if (messages.length) {
        return messages.join(' ');
      }
    }
    return typeof problem.title === 'string' && problem.title.trim() ? problem.title.trim() : null;
  }
}
