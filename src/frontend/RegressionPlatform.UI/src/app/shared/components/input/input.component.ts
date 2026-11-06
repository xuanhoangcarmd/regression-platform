import { NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, forwardRef, input, signal } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';

/**
 * Generic text input with an optional leading icon. Implements
 * ControlValueAccessor, so it binds directly to `[formControl]`/`ngModel`.
 */
@Component({
  selector: 'app-input',
  standalone: true,
  imports: [NgClass, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => InputComponent), multi: true }],
  template: `
    <div class="relative w-full">
      @if (icon()) {
        <mat-icon class="absolute left-3 top-1/2 -translate-y-1/2 text-[18px] text-slate-400">{{ icon() }}</mat-icon>
      }
      <input
        [type]="type()"
        [placeholder]="placeholder()"
        [value]="value()"
        [disabled]="disabled()"
        (input)="onInput($event)"
        (blur)="onTouched()"
        [ngClass]="[
          icon() ? 'pl-9' : 'pl-4',
          error()
            ? 'border-error focus:border-error focus:ring-error'
            : 'border-outline-variant focus:border-primary-container focus:ring-primary-container',
        ]"
        class="w-full rounded-lg border bg-surface-container-lowest py-2 pr-4 text-body-sm text-on-surface outline-none transition-shadow placeholder:text-slate-400 focus:ring-2 disabled:opacity-50"
      />
    </div>
    @if (error()) {
      <p class="mt-1 text-body-sm text-error">{{ error() }}</p>
    }
  `,
})
export class InputComponent implements ControlValueAccessor {
  readonly type = input<string>('text');
  readonly placeholder = input<string>('');
  readonly icon = input<string>('');
  /** Validation message to show below the input (red). Null/empty = valid. */
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
    const value = (event.target as HTMLInputElement).value;
    this.value.set(value);
    this.onChange(value);
  }
}
