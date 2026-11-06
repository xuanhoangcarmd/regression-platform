/**
 * Expected Response (baseline editing): an admin edits a run's captured responses so a later
 * compare can use the edited "expected" body instead of the original one.
 */

/** One API of the run with how many of its responses have a saved expected (edited) version. */
export interface IExpectedApi {
  apiId: number;
  apiName: string;
  method: string;
  endpoint: string;
  requestCount: number;
  editedCount: number;
}

/** One request of the run in the editor list (no bodies). */
export interface IExpectedRequestItem {
  requestId: number;
  apiId: number;
  title: string;
  description: string | null;
  statusCode: number | null;
  /** True when a saved expected (edited) version exists on the server. */
  isEdited: boolean;
  editedBy: string | null;
  editedDate: string | null;
}

/** The saved expected (edited) version of a response. */
export interface IExpectedOverride {
  statusCode: number | null;
  responseJson: string;
  reason: string | null;
  /** Null until authentication exists. */
  updatedBy: string | null;
  updatedDate: string;
}

/** Original captured response + its saved expected version (if any) for one request. */
export interface IExpectedResponseDetail {
  requestId: number;
  apiId: number;
  title: string;
  statusCode: number | null;
  responseJson: string | null;
  errorMessage: string | null;
  override: IExpectedOverride | null;
}

/** An unsaved client-side edit of one request's expected response. */
export interface IExpectedDraft {
  requestId: number;
  statusCode: number | null;
  /** Pretty-printed JSON text being edited (may be invalid while typing in Code view). */
  json: string;
  reason: string;
}

/** One item of the batch save. */
export interface IExpectedSaveItem {
  requestId: number;
  statusCode: number | null;
  responseJson: string;
  reason: string | null;
}

/** Bulk field action applied to several requests' drafts. */
export type BulkFieldAction = 'set' | 'remove';

/** Which bodies a compare uses: each run's edited version when one exists, or the captured ones only. */
export type CompareBaselineMode = 'Expected' | 'Original';
