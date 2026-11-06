import { Directive, TemplateRef, inject, input } from '@angular/core';

import { IDataTableColumn } from './data-table-column.model';

/**
 * Marks an `<ng-template>` as a custom HEADER renderer for a column (e.g. a sort popover).
 * Usage: `<ng-template appHeader="ymme" let-col> … </ng-template>` — `col` is the column config.
 * When present, it replaces the default label/sort button for that column's header cell.
 */
@Directive({
  selector: 'ng-template[appHeader]',
  standalone: true,
})
export class HeaderTemplateDirective {
  /** Column key this header template renders. */
  readonly appHeader = input.required<string>();

  readonly templateRef = inject<TemplateRef<{ $implicit: IDataTableColumn }>>(TemplateRef);
}
