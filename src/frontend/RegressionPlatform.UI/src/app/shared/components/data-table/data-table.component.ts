import { NgClass, NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  TemplateRef,
  computed,
  contentChildren,
  input,
  output,
} from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

import { AppIconsConstants } from '@core/constants';

import { CellTemplateDirective } from './cell-template.directive';
import { HeaderTemplateDirective } from './header-template.directive';
import { IDataTableColumn } from './data-table-column.model';

/**
 * Generic, presentational data table: typed column config + rows, sortable
 * headers (server-side — emits `sortChange`), per-column cell templates
 * (`appCell`), default text cells, and an empty state. Holds no data logic.
 */
@Component({
  selector: 'app-data-table',
  standalone: true,
  imports: [NgClass, NgTemplateOutlet, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './data-table.component.html',
})
export class DataTableComponent {
  readonly columns = input.required<IDataTableColumn[]>();
  readonly rows = input.required<readonly unknown[]>();
  readonly sortKey = input<string | null>(null);
  readonly sortDirection = input<'asc' | 'desc'>('asc');
  readonly emptyMessage = input<string>('No data.');
  /** When true, dims the table and shows a spinner overlay (keeps current rows visible, e.g. while paging). */
  readonly loading = input<boolean>(false);
  readonly sortChange = output<string>();

  private readonly cellTemplates = contentChildren(CellTemplateDirective);
  private readonly headerTemplates = contentChildren(HeaderTemplateDirective);

  private readonly templateMap = computed(() => {
    const map = new Map<string, TemplateRef<{ $implicit: unknown }>>();
    for (const cell of this.cellTemplates()) {
      map.set(cell.appCell(), cell.templateRef);
    }
    return map;
  });

  private readonly headerTemplateMap = computed(() => {
    const map = new Map<string, TemplateRef<{ $implicit: IDataTableColumn }>>();
    for (const header of this.headerTemplates()) {
      map.set(header.appHeader(), header.templateRef);
    }
    return map;
  });

  protected templateFor(key: string): TemplateRef<{ $implicit: unknown }> | null {
    return this.templateMap().get(key) ?? null;
  }

  protected headerTemplateFor(key: string): TemplateRef<{ $implicit: IDataTableColumn }> | null {
    return this.headerTemplateMap().get(key) ?? null;
  }

  protected cellText(row: unknown, key: string): unknown {
    return (row as Record<string, unknown>)[key];
  }

  protected onHeaderClick(column: IDataTableColumn): void {
    if (column.sortable) {
      this.sortChange.emit(column.key);
    }
  }

  protected sortIcon(column: IDataTableColumn): string {
    if (!column.sortable || this.sortKey() !== column.key) {
      return AppIconsConstants.sortNone;
    }
    return this.sortDirection() === 'asc' ? AppIconsConstants.sortAsc : AppIconsConstants.sortDesc;
  }
}
