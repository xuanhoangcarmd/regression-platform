import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { ModalComponent } from '@shared/components';

import { IApiDefinition } from '../../../models/api-definition.model';
import { displayApiId } from '../../../services/api-definition.service';

/** Confirm-delete dialog for an API. Presentational: emits confirm/close. */
@Component({
  selector: 'app-api-remove-modal',
  standalone: true,
  imports: [ModalComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './api-remove-modal.component.html',
})
export class ApiRemoveModalComponent {
  readonly api = input.required<IApiDefinition>();
  readonly loading = input<boolean>(false);
  readonly closed = output<void>();
  readonly confirmed = output<void>();

  protected readonly displayApiId = displayApiId;
}
