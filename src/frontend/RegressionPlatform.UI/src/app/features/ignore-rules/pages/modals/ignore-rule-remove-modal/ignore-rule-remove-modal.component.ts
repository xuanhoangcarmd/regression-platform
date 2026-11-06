import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { ModalComponent } from '@shared/components';

import { IIgnoreRule } from '../../../models/ignore-rule.model';
import { displayRuleId } from '../../../services/ignore-rule.service';

/** Confirm-delete dialog for an ignore rule. */
@Component({
  selector: 'app-ignore-rule-remove-modal',
  standalone: true,
  imports: [ModalComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './ignore-rule-remove-modal.component.html',
})
export class IgnoreRuleRemoveModalComponent {
  readonly rule = input.required<IIgnoreRule>();
  readonly loading = input<boolean>(false);
  readonly closed = output<void>();
  readonly confirmed = output<void>();

  protected readonly displayRuleId = displayRuleId;
}
