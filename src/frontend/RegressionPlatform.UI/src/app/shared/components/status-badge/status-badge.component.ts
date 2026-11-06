import { NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type StatusTone = 'success' | 'error' | 'warning' | 'info' | 'neutral';

/** Generic pill badge for statuses (passed/failed/pending/…). Tone sets the colors. */
@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [NgClass],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      class="inline-flex items-center rounded-full border px-2 py-0.5 text-label-xs font-label-xs"
      [ngClass]="toneClass()"
    >
      {{ label() }}
    </span>
  `,
})
export class StatusBadgeComponent {
  readonly label = input.required<string>();
  readonly tone = input<StatusTone>('neutral');

  private static readonly toneClasses: Record<StatusTone, string> = {
    success: 'bg-green-50 text-green-700 border-green-200',
    error: 'bg-red-50 text-red-700 border-red-200',
    warning: 'bg-amber-50 text-amber-700 border-amber-200',
    info: 'bg-blue-50 text-blue-700 border-blue-200',
    neutral: 'bg-slate-100 text-slate-600 border-slate-200',
  };

  protected readonly toneClass = computed(() => StatusBadgeComponent.toneClasses[this.tone()]);
}
