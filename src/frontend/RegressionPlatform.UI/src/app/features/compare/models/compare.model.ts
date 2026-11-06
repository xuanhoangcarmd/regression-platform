/** Lifecycle status of a compare job (mirrors CompareJob.Status). */
export type CompareStatus = 'Pending' | 'Running' | 'Completed' | 'Failed' | 'Cancelled';

export type CompareStatusFilter = 'all' | CompareStatus;

/** Per-request compare outcome (mirrors CompareDetail.Result). */
export type CompareResultType = 'Unchanged' | 'Changed' | 'Added' | 'Removed' | 'Failed';

export type CompareResultFilter = 'all' | CompareResultType;

/** A compare job row for the History list. Counts are null until the job finishes. */
export interface ICompareJob {
  id: number;
  runAId: number;
  runBId: number;
  runATitle: string;
  runBTitle: string;
  status: CompareStatus;
  progress: number;
  compareResultId: number | null;
  totalRequest: number | null;
  unchangedCount: number | null;
  changedCount: number | null;
  addedCount: number | null;
  removedCount: number | null;
  failedCount: number | null;
  finishedDate: string | null;
  createdDate: string;
  /** True when a report was generated from this compare (so it can't be deleted). */
  hasReport: boolean;
}

/** Compare detail header (job + result summary + run labels). */
export interface ICompareSummary {
  id: number;
  projectId: number;
  runAId: number;
  runBId: number;
  runATitle: string;
  runBTitle: string;
  status: CompareStatus;
  progress: number;
  compareResultId: number | null;
  totalRequest: number | null;
  unchangedCount: number | null;
  changedCount: number | null;
  addedCount: number | null;
  removedCount: number | null;
  failedCount: number | null;
  startedDate: string | null;
  finishedDate: string | null;
  createdDate: string;
  /** Body source: captured only, or each run's edited (expected) version when one exists. */
  baselineMode: CompareBaselineMode;
  /** True = order-insensitive compare (default); false = object-key and array order are significant. */
  ignoreOrder: boolean;
}

/** Which body a compare uses for Run A. */
export type CompareBaselineMode = 'Expected' | 'Original';

/** Result breakdown as percentages (for the History "Result" bar). */
export interface ICompareMetrics {
  unchangedPercent: number;
  changedPercent: number;
  addedPercent: number;
  removedPercent: number;
  failedPercent: number;
}

/** Per-API breakdown of a compare ("Change by API"). */
export interface ICompareApiStatistic {
  apiId: number;
  apiName: string;
  method: string;
  endpoint: string;
  requestCount: number;
  unchangedCount: number;
  changedCount: number;
  addedCount: number;
  removedCount: number;
  failedCount: number;
}

/** One per-request row in the compare detail list. */
export interface ICompareDetailItem {
  requestId: number;
  title: string;
  method: string;
  endpoint: string;
  result: CompareResultType;
  /** An edited (expected) response of either run was used for this request. */
  isEdited: boolean;
}

/** One per-request row for the full export (identity + result + raw field-diff JSON). */
export interface ICompareFullDetail {
  requestId: number;
  title: string;
  method: string;
  endpoint: string;
  result: CompareResultType;
  /** Field-level diffs as a JSON array string; null unless the request Changed. */
  diffJson: string | null;
  /** The request's input that was sent (for the full export). */
  queryStringJson: string | null;
  requestBody: string | null;
}

/**
 * One per-request row for the report's "Test Results" export: identity + input, BOTH runs' raw stored
 * responses (status / duration / error / body) and the compare outcome with its field-diff JSON.
 */
export interface ICompareTestResultRow {
  requestId: number;
  apiName: string;
  method: string;
  endpoint: string;
  title: string;
  description: string | null;
  queryStringJson: string | null;
  requestBody: string | null;
  result: CompareResultType;
  /** Field-level diffs as a JSON array string; null unless the request Changed. */
  diffJson: string | null;
  statusCodeA: number | null;
  statusCodeB: number | null;
  durationMsA: number | null;
  durationMsB: number | null;
  errorMessageA: string | null;
  errorMessageB: string | null;
  responseAJson: string | null;
  responseBJson: string | null;
  /** Run A's columns come from its edited (expected) response. */
  isEdited: boolean;
}

/** Full per-request diff for the side-by-side viewer. */
export interface ICompareDetailDiff {
  requestId: number;
  title: string;
  method: string;
  endpoint: string;
  result: CompareResultType;
  /** Field-level differences as a JSON array string; null when there are none. */
  diffJson: string | null;
  statusCodeA: number | null;
  statusCodeB: number | null;
  /** Full bodies (real values) — for display. */
  responseAJson: string | null;
  responseBJson: string | null;
  /** Bodies with ignore rules applied — used to mark which lines are ignored. */
  responseAComparedJson: string | null;
  responseBComparedJson: string | null;
  errorMessageA: string | null;
  errorMessageB: string | null;
  /** Run A's / Run B's side is its edited (expected) response, not the captured one. */
  isEditedA: boolean;
  isEditedB: boolean;
}

/** One field-level diff entry parsed from {@link ICompareDetailDiff.diffJson}. */
export interface IFieldDiff {
  path: string;
  changeType: string;
  old: unknown;
  new: unknown;
}

/** An old→new value transition in a change pattern. */
export interface ICompareChangeSample {
  old: string | null;
  new: string | null;
}

/** A distinct change pattern (changeType + index-normalized path) aggregated across requests. */
export interface ICompareChangePattern {
  changeType: string;
  path: string;
  occurrences: number;
  requestCount: number;
  distinctTransitions: number;
  samples: ICompareChangeSample[];
}

export interface ICompareChangeTotals {
  requests: number;
  unchanged: number;
  changed: number;
  added: number;
  removed: number;
  failed: number;
}

/** Distinct change patterns for one API (multi-API compares stay distinguishable). */
export interface ICompareApiChangeGroup {
  apiId: number;
  apiName: string | null;
  method: string | null;
  endpoint: string | null;
  requestCount: number;
  patternCount: number;
  patterns: ICompareChangePattern[];
}

/** Compact, distinct summary of what changed in a compare (for review / AI), grouped per API. */
export interface ICompareChangeSummary {
  compareId: number;
  runA: string;
  runB: string;
  totals: ICompareChangeTotals;
  apis: ICompareApiChangeGroup[];
}

/** Payload to create a compare. Empty `apiIds` = compare all of the project's requests. */
export interface ICompareCreate {
  runAId: number;
  runBId: number;
  apiIds: number[];
  /**
   * Run A body source: 'Expected' = the edited expected response when one exists (else the original),
   * 'Original' = always the captured response (default when omitted).
   */
  baselineMode?: CompareBaselineMode;
  /** Omitted / true = ignore order (default); false = strict order. */
  ignoreOrder?: boolean;
}
