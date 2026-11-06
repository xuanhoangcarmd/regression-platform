import { NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, forwardRef, input, signal } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

/**
 * Generic multi-line text input. Implements ControlValueAccessor, so it binds
 * directly to `[formControl]`/`ngModel`. Use for JSON/body fields.
 */
@Component({
  selector: 'app-textarea',
  standalone: true,
  imports: [NgClass],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => TextareaComponent), multi: true }],
  template: `
    <textarea
      [rows]="rows()"
      [placeholder]="placeholder()"
      [value]="value()"
      [disabled]="disabled()"
      (input)="onInput($event)"
      (blur)="onTouched()"
      [ngClass]="
        error()
          ? 'border-error focus:border-error focus:ring-error'
          : 'border-outline-variant focus:border-primary-container focus:ring-primary-container'
      "
      class="w-full resize-y rounded-lg border bg-surface-container-lowest px-3 py-2 font-code-sm text-code-sm text-on-surface outline-none transition-shadow placeholder:font-body-sm placeholder:text-slate-400 focus:ring-2 disabled:opacity-50"
    ></textarea>
    @if (error()) {
      <p class="mt-1 text-body-sm text-error">{{ error() }}</p>
    }
  `,
})
export class TextareaComponent implements ControlValueAccessor {
  readonly placeholder = input<string>('');
  readonly rows = input<number>(4);
  readonly error = input<string | null>(null);

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

  protected onInput(event: Event): void {
    const value = (event.target as HTMLTextAreaElement).value;
    this.value.set(value);
    this.onChange(value);
  }
}
