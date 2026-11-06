import { ChangeDetectionStrategy, Component, OnInit, inject, input, output } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { InputComponent, ModalComponent, TextareaComponent } from '@shared/components';
import { controlErrorMessage } from '@shared/utils';

import { IDataset, IDatasetUpdate } from '../../../models/dataset.model';

/** Create/edit a dataset (name, description, active state). */
@Component({
  selector: 'app-dataset-form-modal',
  standalone: true,
  imports: [ReactiveFormsModule, ModalComponent, InputComponent, TextareaComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dataset-form-modal.component.html',
})
export class DatasetFormModalComponent implements OnInit {
  readonly dataset = input<IDataset | null>(null);
  readonly loading = input<boolean>(false);
  readonly closed = output<void>();
  readonly submitted = output<IDatasetUpdate>();

  private readonly formBuilder = inject(NonNullableFormBuilder);

  protected readonly controlErrorMessage = controlErrorMessage;
  protected isEdit = false;

  protected readonly form = this.formBuilder.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.maxLength(1000)]],
    isActive: [true],
  });

  ngOnInit(): void {
    const dataset = this.dataset();
    if (dataset) {
      this.isEdit = true;
      this.form.setValue({
        name: dataset.name,
        description: dataset.description ?? '',
        isActive: dataset.isActive,
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
      name: value.name.trim(),
      description: value.description.trim() || null,
      isActive: value.isActive,
    });
  }
}
