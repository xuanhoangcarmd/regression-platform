/** Column definition for the generic data table. `key` is used to look up a cell
 *  template and (for sortable columns) is emitted on sort. */
export interface IDataTableColumn {
  key: string;
  label: string;
  sortable?: boolean;
  align?: 'right' | 'center';
  widthClass?: string;
  /** Optional CSS class for the header cell (e.g. a color); overrides the default muted color. */
  headerClass?: string;
}
