import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import { MatIconModule } from '@angular/material/icon';

import { AppIconsConstants } from '@core/constants';
import {
  ButtonComponent,
  CellTemplateDirective,
  DataTableComponent,
  IDataTableColumn,
  StatusBadgeComponent,
} from '@shared/components';

import { IIgnoreRule, IIgnoreRuleWrite } from '../../models/ignore-rule.model';
import { IgnoreRuleService, displayRuleId } from '../../services/ignore-rule.service';
import { IgnoreRuleFormModalComponent } from '../modals/ignore-rule-form-modal/ignore-rule-form-modal.component';
import { IgnoreRuleRemoveModalComponent } from '../modals/ignore-rule-remove-modal/ignore-rule-remove-modal.component';

/** Ignore Rules: fields skipped during response comparison. */
@Component({
  selector: 'app-ignore-rule-list-page',
  standalone: true,
  imports: [
    MatIconModule,
    ButtonComponent,
    DataTableComponent,
    CellTemplateDirective,
    StatusBadgeComponent,
    IgnoreRuleFormModalComponent,
    IgnoreRuleRemoveModalComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './ignore-rule-list-page.component.html',
})
export class IgnoreRuleListPageComponent {
  private readonly service = inject(IgnoreRuleService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly rules = this.service.rules;
  protected readonly loading = this.service.loading;
  /** API options for the "Applies To" multi-select (project-scoped; owned by the service). */
  protected readonly apiOptions = this.service.apiOptions;
  protected readonly displayRuleId = displayRuleId;

  protected readonly formOpen = signal(false);
  protected readonly editingRule = signal<IIgnoreRule | null>(null);
  protected readonly removingRule = signal<IIgnoreRule | null>(null);
  /** True while a create/update/delete is in flight (drives the modal confirm spinner). */
  protected readonly actionLoading = signal(false);

  protected readonly addIcon = AppIconsConstants.add;
  protected readonly editIcon = AppIconsConstants.edit;
  protected readonly deleteIcon = AppIconsConstants.delete;

  protected readonly columns: IDataTableColumn[] = [
    { key: 'id', label: 'Rule ID', widthClass: 'w-[120px]' },
    { key: 'jsonPath', label: 'Json Path', widthClass: 'w-[200px]' },
    { key: 'description', label: 'Description' },
    { key: 'appliesTo', label: 'Applies To', widthClass: 'w-[160px]' },
    { key: 'enabled', label: 'Status', widthClass: 'w-[120px]' },
    { key: 'actions', label: 'Actions', align: 'right', widthClass: 'w-[100px]' },
  ];

  protected asRule(row: unknown): IIgnoreRule {
    return row as IIgnoreRule;
  }

  /** "All APIs" when no APIs are linked, otherwise the comma-separated names. */
  protected appliesToLabel(rule: IIgnoreRule): string {
    return rule.apis.length === 0 ? 'All APIs' : rule.apis.map((api) => api.name).join(', ');
  }

  openAdd(): void {
    this.editingRule.set(null);
    this.formOpen.set(true);
  }

  openEdit(row: unknown): void {
    this.editingRule.set(this.asRule(row));
    this.formOpen.set(true);
  }

  onSubmitted(payload: IIgnoreRuleWrite): void {
    const editing = this.editingRule();
    const startedAt = Date.now();
    this.actionLoading.set(true);
    const request$: Observable<unknown> = editing
      ? this.service.update(editing.id, payload)
      : this.service.create(payload);
    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.finishAction(startedAt, () => this.formOpen.set(false)),
      error: () => this.finishAction(startedAt),
    });
  }

  openRemove(row: unknown): void {
    this.removingRule.set(this.asRule(row));
  }

  onRemoveConfirmed(rule: IIgnoreRule): void {
    const startedAt = Date.now();
    this.actionLoading.set(true);
    this.service
      .remove(rule.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.finishAction(startedAt, () => this.removingRule.set(null)),
        error: () => this.finishAction(startedAt),
      });
  }

  /**
   * Minimum time (ms) the confirm spinner stays visible — on dev the API is local and near-instant;
   * without a floor the spinner flashes and users think nothing happened.
   */
  private static readonly MIN_LOADING_MS = 500;

  private finishAction(startedAt: number, onDone?: () => void): void {
    const remaining = IgnoreRuleListPageComponent.MIN_LOADING_MS - (Date.now() - startedAt);
    const run = () => {
      this.actionLoading.set(false);
      onDone?.();
    };
    if (remaining > 0) {
      setTimeout(run, remaining);
    } else {
      run();
    }
  }
}
