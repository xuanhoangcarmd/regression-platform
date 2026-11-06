import { NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

import { AppIconsConstants } from '@core/constants';

/**
 * Numbered paginator (range text + page buttons with ellipsis + prev/next).
 * Presentational: parent owns the page state and reacts to (pageChange).
 */
@Component({
  selector: 'app-paginator',
  standalone: true,
  imports: [NgClass, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './paginator.component.html',
})
export class PaginatorComponent {
  readonly pageNumber = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly totalCount = input.required<number>();
  readonly pageChange = output<number>();

  protected readonly chevronLeftIcon = AppIconsConstants.chevronLeft;
  protected readonly chevronRightIcon = AppIconsConstants.chevronRight;

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));
  readonly rangeStart = computed(() => (this.totalCount() === 0 ? 0 : (this.pageNumber() - 1) * this.pageSize() + 1));
  readonly rangeEnd = computed(() => Math.min(this.pageNumber() * this.pageSize(), this.totalCount()));

  /** Page buttons to render; 'ellipsis' marks a gap. */
  readonly pages = computed<(number | 'ellipsis')[]>(() => {
    const total = this.totalPages();
    const current = this.pageNumber();
    if (total <= 7) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }

    let start = Math.max(2, current - 1);
    let end = Math.min(total - 1, current + 1);
    if (current <= 3) {
      start = 2;
      end = 4;
    }
    if (current >= total - 2) {
      start = total - 3;
      end = total - 1;
    }

    const out: (number | 'ellipsis')[] = [1];
    if (start > 2) {
      out.push('ellipsis');
    }
    for (let p = start; p <= end; p++) {
      out.push(p);
    }
    if (end < total - 1) {
      out.push('ellipsis');
    }
    out.push(total);
    return out;
  });

  go(page: number): void {
    const clamped = Math.min(Math.max(1, page), this.totalPages());
    if (clamped !== this.pageNumber()) {
      this.pageChange.emit(clamped);
    }
  }
}
