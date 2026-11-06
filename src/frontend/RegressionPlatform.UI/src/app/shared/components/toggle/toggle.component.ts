import { NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, forwardRef, input, signal } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

/**
 * On/off switch (toggle). Implements ControlValueAccessor, so it binds directly
 * to `[formControl]`/`ngModel` with a boolean value.
 */
@Component({
  selector: 'app-toggle',
  standalone: true,
  imports: [NgClass],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => ToggleComponent), multi: true }],
  template: `
    <button
      type="button"
      role="switch"
      [attr.aria-checked]="checked()"
      [attr.aria-label]="ariaLabel() || null"
      [disabled]="disabled()"
      (click)="toggle()"
      (blur)="onTouched()"
      class="relative inline-flex h-6 w-11 shrink-0 rounded-full transition-colors disabled:cursor-not-allowed disabled:opacity-50"
      [ngClass]="checked() ? 'bg-primary-container' : 'bg-slate-300'"
    >
      <span
        class="absolute left-0.5 top-0.5 h-5 w-5 rounded-full bg-white transition-transform"
        [ngClass]="checked() ? 'translate-x-5' : ''"
      ></span>
    </button>
  `,
})
export class ToggleComponent implements ControlValueAccessor {
  readonly ariaLabel = input<string>('');

  protected readonly checked = signal(false);
  protected readonly disabled = signal(false);

  private onChange: (value: boolean) => void = () => undefined;
  protected onTouched: () => void = () => undefined;

  writeValue(value: boolean | null): void {
    this.checked.set(!!value);
  }

  registerOnChange(fn: (value: boolean) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }

  protected toggle(): void {
    if (this.disabled()) {
      return;
    }
    const next = !this.checked();
    this.checked.set(next);
    this.onChange(next);
    this.onTouched();
  }
}
