import { ChangeDetectionStrategy, Component, OnInit, inject, input, output } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { InputComponent, ISelectOption, ModalComponent, SelectComponent, ToggleComponent } from '@shared/components';
import { controlErrorMessage } from '@shared/utils';

import { ApiHttpMethod, IApiDefinition } from '../../../models/api-definition.model';
import { displayApiId } from '../../../services/api-definition.service';

/** Edit-API dialog. Presentational: receives the API, emits the edited copy. */
@Component({
  selector: 'app-api-edit-modal',
  standalone: true,
  imports: [ReactiveFormsModule, ModalComponent, InputComponent, SelectComponent, ToggleComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './api-edit-modal.component.html',
})
export class ApiEditModalComponent implements OnInit {
  readonly api = input.required<IApiDefinition>();
  readonly loading = input<boolean>(false);
  readonly closed = output<void>();
  readonly saved = output<IApiDefinition>();

  private readonly formBuilder = inject(NonNullableFormBuilder);

  protected readonly displayApiId = displayApiId;
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

  ngOnInit(): void {
    const api = this.api();
    this.form.setValue({ name: api.name, method: api.method, endpoint: api.endpoint, isActive: api.isActive });
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { name, method, endpoint, isActive } = this.form.getRawValue();
    this.saved.emit({ ...this.api(), name, method, endpoint, isActive });
  }
}
