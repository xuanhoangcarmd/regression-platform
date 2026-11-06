import { NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, HostListener, computed, input, output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

import { AppIconsConstants } from '@core/constants';

import { ButtonComponent, ButtonVariant } from '../button/button.component';

export type ModalSize = 'sm' | 'md' | 'lg';
/** `update` → primary confirm button; `remove` → red confirm + warning icon. */
export type ModalType = 'update' | 'remove';

/**
 * Reusable confirm modal: backdrop + panel + header + projected body + a footer
 * (cancel + confirm) styled by `type`. Project any content/component as the body:
 *
 *   <app-modal type="remove" title="Delete?" confirmLabel="Delete"
 *              (confirm)="…" (cancelled)="…">
 *     <my-content />
 *   </app-modal>
 *
 * Closes (cancelled) on backdrop, ✕, and Escape. Holds no business logic.
 */
@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [NgClass, MatIconModule, ButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (open()) {
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4">
        <!-- Backdrop is a real button so it's focusable/keyboard-dismissable; panel is a sibling. -->
        <button
          type="button"
          aria-label="Close"
          class="fixed inset-0 bg-slate-900/40"
          (click)="onCancel()"
        ></button>
        <div
          role="dialog"
          aria-modal="true"
          [attr.aria-label]="title()"
          class="relative z-10 flex max-h-[90vh] w-full flex-col rounded-xl bg-surface-container-lowest shadow-xl"
          [ngClass]="sizeClass()"
        >
          <!-- Header -->
          <div class="flex items-center justify-between border-b border-outline-variant px-5 py-4">
            <h2 class="text-title-sm font-title-sm font-semibold text-on-surface">{{ title() }}</h2>
            <button
              type="button"
              (click)="onCancel()"
              aria-label="Close"
              class="text-secondary transition-colors hover:text-on-surface"
            >
              <mat-icon class="text-[20px]">{{ closeIcon }}</mat-icon>
            </button>
          </div>

          <!-- Body (scrolls when content is taller than the panel; header/footer stay fixed) -->
          <div class="flex-1 overflow-y-auto px-5 py-4" [class.flex]="type() === 'remove'" [class.gap-3]="type() === 'remove'">
            @if (type() === 'remove') {
              <mat-icon class="mt-0.5 shrink-0 text-[24px] text-error">{{ warningIcon }}</mat-icon>
            }
            <div [class.flex-1]="type() === 'remove'">
              <ng-content />
            </div>
          </div>

          <!-- Footer -->
          <div class="flex justify-end gap-3 border-t border-outline-variant px-5 py-4">
            <app-button variant="secondary" [disabled]="confirmLoading()" (click)="onCancel()">{{ cancelLabel() }}</app-button>
            <app-button
              [variant]="confirmVariant()"
              [disabled]="confirmDisabled()"
              [loading]="confirmLoading()"
              (click)="confirm.emit()"
            >
              {{ confirmLabel() }}
            </app-button>
          </div>
        </div>
      </div>
    }
  `,
})
export class ModalComponent {
  readonly open = input<boolean>(true);
  readonly title = input<string>('');
  readonly type = input<ModalType>('update');
  readonly size = input<ModalSize>('md');
  readonly confirmLabel = input<string>('Save');
  readonly cancelLabel = input<string>('Cancel');
  readonly confirmDisabled = input<boolean>(false);
  /** When true, the confirm button shows a spinner and cancel/backdrop/Escape are blocked. */
  readonly confirmLoading = input<boolean>(false);

  readonly confirm = output<void>();
  readonly cancelled = output<void>();

  /** Ignore close requests (backdrop / ✕ / Escape / Cancel) while the confirm action is running. */
  protected onCancel(): void {
    if (this.confirmLoading()) {
      return;
    }
    this.cancelled.emit();
  }

  protected readonly closeIcon = AppIconsConstants.close;
  protected readonly warningIcon = AppIconsConstants.warning;

  protected readonly confirmVariant = computed<ButtonVariant>(() => (this.type() === 'remove' ? 'danger' : 'primary'));

  private static readonly sizes: Record<ModalSize, string> = {
    sm: 'max-w-sm',
    md: 'max-w-md',
    lg: 'max-w-lg',
  };

  protected readonly sizeClass = computed(() => ModalComponent.sizes[this.size()]);

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    if (this.open()) {
      this.onCancel();
    }
  }
}
