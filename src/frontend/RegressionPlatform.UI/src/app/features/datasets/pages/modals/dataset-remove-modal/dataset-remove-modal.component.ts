import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { ModalComponent } from '@shared/components';

import { IDatasetItem } from '../../../models/dataset.model';

/** Confirm-delete dialog for a dataset. Deletion is blocked server-side while it still has requests/runs. */
@Component({
  selector: 'app-dataset-remove-modal',
  standalone: true,
  imports: [ModalComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dataset-remove-modal.component.html',
})
export class DatasetRemoveModalComponent {
  readonly dataset = input.required<IDatasetItem>();
  readonly loading = input<boolean>(false);
  readonly closed = output<void>();
  readonly confirmed = output<void>();
}
