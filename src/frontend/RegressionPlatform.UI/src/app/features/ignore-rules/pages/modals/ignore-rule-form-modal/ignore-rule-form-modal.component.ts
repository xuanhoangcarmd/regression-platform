import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { ISelectOption, InputComponent, ModalComponent, SelectComponent, TextareaComponent, ToggleComponent } from '@shared/components';
import { controlErrorMessage } from '@shared/utils';

import { IIgnoreRule, IIgnoreRuleWrite, IgnoreRuleType } from '../../../models/ignore-rule.model';

/** Create/edit a compare rule (ignore field / extract root). */
@Component({
  selector: 'app-ignore-rule-form-modal',
  standalone: true,
  imports: [ReactiveFormsModule, ModalComponent, InputComponent, SelectComponent, TextareaComponent, ToggleComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './ignore-rule-form-modal.component.html',
})
export class IgnoreRuleFormModalComponent implements OnInit {
  readonly rule = input<IIgnoreRule | null>(null);
  /** All APIs to choose from (value = id as string, label = name). */
  readonly apiOptions = input<ISelectOption[]>([]);
  readonly loading = input<boolean>(false);
  readonly closed = output<void>();
  readonly submitted = output<IIgnoreRuleWrite>();

  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly controlErrorMessage = controlErrorMessage;
  protected isEdit = false;

  protected readonly ruleTypeOptions: ISelectOption[] = [
    { value: 'Ignore', label: 'Ignore field' },
    { value: 'ExtractRoot', label: 'Extract root (unwrap)' },
    { value: 'TrimAfter', label: 'Cut value at character' },
  ];

  /** Mirrors the rule-type control so the template can switch its help text reactively. */
  protected readonly ruleType = signal<IgnoreRuleType>('Ignore');

  /** Selected API ids (empty = applies to all APIs). */
  protected readonly selectedApiIds = signal<Set<number>>(new Set());

  // JSON path like $.property.sub, $.array[0].property, or $.array[*].property ([*] = every element).
  private static readonly jsonPathPattern = /^\$(\.[A-Za-z_$][\w$]*|\[\d+\]|\[\*\])+$/;

  protected readonly form = this.formBuilder.group({
    jsonPath: ['', [Validators.required, Validators.maxLength(500), Validators.pattern(IgnoreRuleFormModalComponent.jsonPathPattern)]],
    description: ['', [Validators.maxLength(500)]],
    ruleType: ['Ignore' as IgnoreRuleType],
    /** TrimAfter only — required (and validated) only while that type is selected. */
    delimiter: ['?', [Validators.maxLength(50)]],
    enabled: [true],
  });

  ngOnInit(): void {
    // Keep the help-text signal in sync with the control (no signal writes inside effect()).
    this.form.controls.ruleType.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => {
        this.ruleType.set(value);
        this.syncDelimiterValidators(value);
      });

    const rule = this.rule();
    if (rule) {
      this.isEdit = true;
      this.form.setValue({
        jsonPath: rule.jsonPath,
        description: rule.description ?? '',
        ruleType: rule.ruleType,
        delimiter: rule.delimiter ?? '?',
        enabled: rule.enabled,
      });
      this.ruleType.set(rule.ruleType);
      this.syncDelimiterValidators(rule.ruleType);
      this.selectedApiIds.set(new Set(rule.apis.map((api) => api.id)));
    }
  }

  /** The delimiter is required only for "Cut value at character". */
  private syncDelimiterValidators(type: IgnoreRuleType): void {
    const control = this.form.controls.delimiter;
    control.setValidators(type === 'TrimAfter' ? [Validators.required, Validators.maxLength(50)] : [Validators.maxLength(50)]);
    control.updateValueAndValidity({ emitEvent: false });
  }

  protected isApiSelected(id: number): boolean {
    return this.selectedApiIds().has(id);
  }

  protected toggleApi(id: number): void {
    this.selectedApiIds.update((current) => {
      const next = new Set(current);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  }

  protected clearApis(): void {
    this.selectedApiIds.set(new Set());
  }

  protected apiId(option: ISelectOption): number {
    return Number(option.value);
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.submitted.emit({
      jsonPath: value.jsonPath.trim(),
      description: value.description.trim() || null,
      ruleType: value.ruleType,
      delimiter: value.ruleType === 'TrimAfter' ? value.delimiter : null,
      enabled: value.enabled,
      apiIds: [...this.selectedApiIds()],
    });
  }
}
