import { Directive, TemplateRef, inject, input } from '@angular/core';

/**
 * Marks an `<ng-template>` as the cell renderer for a column.
 * Usage: `<ng-template appCell="status" let-row> … </ng-template>`.
 */
@Directive({
  selector: 'ng-template[appCell]',
  standalone: true,
})
export class CellTemplateDirective {
  /** Column key this template renders. */
  readonly appCell = input.required<string>();

  readonly templateRef = inject<TemplateRef<{ $implicit: unknown }>>(TemplateRef);
}
