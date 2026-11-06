import { ChangeDetectionStrategy, Component, DestroyRef, inject, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';

import { InputComponent, ModalComponent } from '@shared/components';
import { controlErrorMessage } from '@shared/utils';

import { IApiImportResult } from '../../../models/api-definition.model';
import { ApiDefinitionService } from '../../../services/api-definition.service';

/** Import-from-Swagger dialog: enter a spec URL, import directly, show the result counts. */
@Component({
  selector: 'app-api-import-modal',
  standalone: true,
  imports: [ReactiveFormsModule, ModalComponent, InputComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './api-import-modal.component.html',
})
export class ApiImportModalComponent {
  readonly closed = output<void>();
  readonly imported = output<IApiImportResult>();

  private readonly service = inject(ApiDefinitionService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly controlErrorMessage = controlErrorMessage;

  protected readonly urlControl = new FormControl<string>('', {
    nonNullable: true,
    validators: [Validators.required, Validators.pattern(/^https?:\/\/.+/i)],
  });

  protected readonly loading = signal(false);
  protected readonly result = signal<IApiImportResult | null>(null);
  protected readonly errorMessage = signal<string | null>(null);

  protected submit(): void {
    if (this.urlControl.invalid) {
      this.urlControl.markAsTouched();
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    this.service
      .importFromSwagger(this.urlControl.value)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          this.result.set(result);
          this.imported.emit(result);
        },
        error: () => {
          this.loading.set(false);
          this.errorMessage.set('Import failed. Check the Swagger URL or the backend.');
        },
      });
  }
}
