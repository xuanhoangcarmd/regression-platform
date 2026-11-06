import { NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

export type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger';
export type ButtonSize = 'sm' | 'md';

/**
 * Generic button. Label is projected (`<app-button>Save</app-button>`); the icon
 * is passed via `icon` (Material Symbols name). Variant/size drive the styling.
 */
@Component({
  selector: 'app-button',
  standalone: true,
  imports: [NgClass, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button [type]="type()" [disabled]="disabled() || loading()" [ngClass]="[base, variantClass(), sizeClass()]">
      @if (loading()) {
        <mat-icon class="animate-spin text-[16px]">progress_activity</mat-icon>
      } @else if (icon() && iconPosition() === 'leading') {
        <mat-icon class="text-[16px]">{{ icon() }}</mat-icon>
      }
      <ng-content />
      @if (icon() && iconPosition() === 'trailing' && !loading()) {
        <mat-icon class="text-[16px]">{{ icon() }}</mat-icon>
      }
    </button>
  `,
})
export class ButtonComponent {
  readonly variant = input<ButtonVariant>('primary');
  readonly size = input<ButtonSize>('md');
  readonly type = input<'button' | 'submit' | 'reset'>('button');
  readonly icon = input<string>('');
  readonly iconPosition = input<'leading' | 'trailing'>('leading');
  readonly disabled = input<boolean>(false);
  readonly loading = input<boolean>(false);

  protected readonly base =
    'inline-flex items-center justify-center gap-1.5 rounded-lg font-medium transition-colors disabled:cursor-not-allowed disabled:opacity-50';

  private static readonly variants: Record<ButtonVariant, string> = {
    primary: 'bg-primary-container text-white shadow-sm hover:bg-primary',
    secondary:
      'border border-outline-variant bg-surface-container-lowest text-on-surface hover:bg-surface-container-low',
    ghost: 'text-primary hover:bg-surface-container-low',
    danger: 'bg-error text-on-error shadow-sm hover:opacity-90',
  };

  private static readonly sizes: Record<ButtonSize, string> = {
    sm: 'px-3 py-1.5 text-body-sm',
    md: 'px-3.5 py-2 text-body-sm',
  };

  protected readonly variantClass = computed(() => ButtonComponent.variants[this.variant()]);
  protected readonly sizeClass = computed(() => ButtonComponent.sizes[this.size()]);
}
