import { ChangeDetectionStrategy, Component, OnInit, inject, input, output } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { InputComponent, ISelectOption, ModalComponent, SelectComponent, TextareaComponent } from '@shared/components';
import { controlErrorMessage } from '@shared/utils';

import { IApiRequestDetail, IApiRequestWrite } from '../../../models/api-request.model';

/** Create/edit a pooled request. Edit emits the same payload — the server versions it. */
@Component({
  selector: 'app-api-request-form-modal',
  standalone: true,
  imports: [ReactiveFormsModule, ModalComponent, InputComponent, SelectComponent, TextareaComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './api-request-form-modal.component.html',
})
export class ApiRequestFormModalComponent implements OnInit {
  readonly request = input<IApiRequestDetail | null>(null);
  readonly apiOptions = input<ISelectOption[]>([]);
  readonly loading = input<boolean>(false);
  readonly closed = output<void>();
  readonly submitted = output<IApiRequestWrite>();

  private readonly formBuilder = inject(NonNullableFormBuilder);

  protected readonly controlErrorMessage = controlErrorMessage;
  protected isEdit = false;

  protected readonly form = this.formBuilder.group({
    apiId: ['', [Validators.required]],
    title: ['', [Validators.required, Validators.maxLength(300)]],
    description: ['', [Validators.maxLength(1000)]],
    headersJson: [''],
    queryStringJson: [''],
    requestBody: [''],
  });

  ngOnInit(): void {
    const request = this.request();
    if (request) {
      this.isEdit = true;
      this.form.setValue({
        apiId: String(request.apiId),
        title: request.title,
        description: request.description ?? '',
        headersJson: request.headersJson ?? '',
        queryStringJson: request.queryStringJson ?? '',
        requestBody: request.requestBody ?? '',
      });
    }
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.submitted.emit({
      apiId: Number(value.apiId),
      title: value.title,
      description: value.description.trim() || null,
      headersJson: value.headersJson.trim() || null,
      queryStringJson: value.queryStringJson.trim() || null,
      requestBody: value.requestBody.trim() || null,
    });
  }
}
