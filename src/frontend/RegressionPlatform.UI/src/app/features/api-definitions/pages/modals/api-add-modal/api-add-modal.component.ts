import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { InputComponent, ISelectOption, ModalComponent, SelectComponent, ToggleComponent } from '@shared/components';
import { controlErrorMessage } from '@shared/utils';

import { ApiDefinitionCreate, ApiHttpMethod } from '../../../models/api-definition.model';

/** Add-API dialog. Presentational: emits the new API's fields on confirm. */
@Component({
  selector: 'app-api-add-modal',
  standalone: true,
  imports: [ReactiveFormsModule, ModalComponent, InputComponent, SelectComponent, ToggleComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './api-add-modal.component.html',
})
export class ApiAddModalComponent {
  readonly loading = input<boolean>(false);
  readonly closed = output<void>();
  readonly created = output<ApiDefinitionCreate>();

  private readonly formBuilder = inject(NonNullableFormBuilder);

  protected readonly controlErrorMessage = controlErrorMessage;

  protected readonly methodOptions: ISelectOption[] = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE'].map((m) => ({
    value: m,
    label: m,
  }));

  protected readonly form = this.formBuilder.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    method: ['GET' as ApiHttpMethod],
    endpoint: ['', [Validators.required, Validators.maxLength(500)]],
    isActive: [true],
  });

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.created.emit(this.form.getRawValue());
  }
}
