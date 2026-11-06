import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';

import { ISelectOption, ModalComponent, SelectComponent } from '@shared/components';

/**
 * Delete all data of one API within a dataset: pick the API, then confirm. Two steps — the pick modal,
 * then a small confirm modal (type="remove"). Emits the chosen API id on confirm.
 */
@Component({
  selector: 'app-api-request-bulk-delete-modal',
  standalone: true,
  imports: [ReactiveFormsModule, ModalComponent, SelectComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './api-request-bulk-delete-modal.component.html',
})
export class ApiRequestBulkDeleteModalComponent {
  /** APIs available to purge (value = id as string, label = name). */
  readonly apiOptions = input<ISelectOption[]>([]);
  /** True while the delete is in flight (drives the confirm spinner). */
  readonly loading = input<boolean>(false);
  readonly closed = output<void>();
  /** The chosen API id to delete all data for. */
  readonly confirmed = output<number>();

  protected readonly apiControl = new FormControl<string>('', { nonNullable: true });
  protected readonly selectedApiId = toSignal(this.apiControl.valueChanges, { initialValue: '' });

  /** True on the confirm step (the small "are you sure" modal). */
  protected readonly confirming = signal(false);

  /** Human-readable name of the picked API (for the confirm message). */
  protected readonly selectedApiLabel = computed(
    () => this.apiOptions().find((option) => option.value === this.selectedApiId())?.label ?? '',
  );

  protected proceed(): void {
    if (this.apiControl.value) {
      this.confirming.set(true);
    }
  }

  protected back(): void {
    this.confirming.set(false);
  }

  protected confirm(): void {
    const id = Number(this.apiControl.value);
    if (id) {
      this.confirmed.emit(id);
    }
  }
}
