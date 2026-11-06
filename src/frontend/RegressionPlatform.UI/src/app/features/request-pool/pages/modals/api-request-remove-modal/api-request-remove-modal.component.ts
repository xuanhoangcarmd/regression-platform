import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { ModalComponent } from '@shared/components';

import { IApiRequestItem } from '../../../models/api-request.model';
import { displayRequestId } from '../../../services/api-request.service';

/** Confirm-archive (soft-delete) dialog for a pooled request. */
@Component({
  selector: 'app-api-request-remove-modal',
  standalone: true,
  imports: [ModalComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './api-request-remove-modal.component.html',
})
export class ApiRequestRemoveModalComponent {
  readonly request = input.required<IApiRequestItem>();
  readonly loading = input<boolean>(false);
  readonly closed = output<void>();
  readonly confirmed = output<void>();

  protected readonly displayRequestId = displayRequestId;
}
