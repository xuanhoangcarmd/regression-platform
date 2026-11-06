import { NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, forwardRef, input, signal } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

export interface ISelectOption {
  value: string;
  label: string;
}

/**
 * Generic native select. Options are passed via `options`; implements
 * ControlValueAccessor, so it binds directly to `[formControl]`/`ngModel`.
 */
@Component({
  selector: 'app-select',
  standalone: true,
  imports: [NgClass],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => SelectComponent), multi: true }],
  template: `
    <select
      [disabled]="disabled()"
      (change)="onSelect($event)"
      (blur)="onTouched()"
      [ngClass]="[
        error()
          ? 'border-error focus:border-error focus:ring-error'
          : 'border-outline-variant focus:border-primary-container focus:ring-primary-container',
        fullWidth() ? '' : 'sm:w-auto',
      ]"
      class="w-full rounded-lg border bg-surface-container-lowest py-2 pl-3 pr-8 text-body-sm text-on-surface outline-none focus:ring-2 disabled:opacity-50"
    >
      @if (placeholder()) {
        <option value="" disabled [selected]="value() === ''">{{ placeholder() }}</option>
      }
      @for (option of options(); track option.value) {
        <option [value]="option.value" [selected]="option.value === value()">{{ option.label }}</option>
      }
    </select>
    @if (error()) {
      <p class="mt-1 text-body-sm text-error">{{ error() }}</p>
    }
  `,
})
export class SelectComponent implements ControlValueAccessor {
  readonly options = input<ISelectOption[]>([]);
  readonly placeholder = input<string>('');
  readonly error = input<string | null>(null);
  /** Force full width (no `sm:w-auto`) — for form fields; default keeps the compact toolbar sizing. */
  readonly fullWidth = input<boolean>(false);

  protected readonly value = signal('');
  protected readonly disabled = signal(false);

  private onChange: (value: string) => void = () => undefined;
  protected onTouched: () => void = () => undefined;

  writeValue(value: string | null): void {
    this.value.set(value ?? '');
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }

  protected onSelect(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.value.set(value);
    this.onChange(value);
  }
}
