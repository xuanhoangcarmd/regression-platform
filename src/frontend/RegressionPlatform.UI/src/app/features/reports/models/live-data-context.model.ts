/**
 * A Live Data Result "context field" rule: when a changed field matches {@link target}, the export
 * also shows the values of {@link fields} so the reader knows WHICH item changed.
 *
 * Example: target `$.VehicleData.TPMSData.Dtcs[*].Definition` + field `$.VehicleData.TPMSData.Dtcs[*].DTC`
 * → the row for `$.VehicleData.TPMSData.Dtcs[3].Definition` shows `DTC: C0750` (read from `Dtcs[3]`).
 */
export interface ILiveDataContextRule {
  /** JSON path of the changed field; `[*]` matches any array index. */
  target: string;
  /**
   * Extra fields to show. Each `[*]` takes the index matched by the target's `[*]` (in order).
   * A path not starting with `$` is relative to the target's last `[*]` item (e.g. just `DTC`).
   */
  fields: string[];
}
