/** Lifecycle status of a regression run (mirrors Run.Status). Queued = started, awaiting the worker. */
export type RunStatus = 'Pending' | 'Queued' | 'Running' | 'Completed' | 'Failed' | 'Cancelled';

export type RunStatusFilter = 'all' | RunStatus;

/** Pass/warning/fail percentages for a finished run (for the metrics bar). */
export interface IRunMetrics {
  passPercent: number;
  warningPercent: number;
  failPercent: number;
}

/** Per-API execution statistics within a run (detail Summary). */
export interface IApiRunStatistic {
  apiId: number;
  apiName: string;
  method: string;
  endpoint: string;
  requestCount: number;
  passedCount: number;
  warningCount: number;
  failedCount: number;
  avgResponseTimeMs: number | null;
}

/** Full detail of a run (Summary tab). */
export interface IRunDetail {
  id: number;
  title: string;
  description: string | null;
  environment: string;
  runType: string;
  status: RunStatus;
  totalRequest: number | null;
  passCount: number | null;
  warningCount: number | null;
  failedCount: number | null;
  startTime: string | null;
  endTime: string | null;
  durationMs: number | null;
  createdDate: string;
  apiCount: number;
  apiStatistics: IApiRunStatistic[];
}

/** One executed request in a run (Requests tab row). */
export interface IRunResponseItem {
  requestId: number;
  title: string;
  method: string;
  endpoint: string;
  statusCode: number | null;
  durationMs: number | null;
  responseSize: number | null;
  status: 'Passed' | 'Warning' | 'Failed';
  errorMessage: string | null;
  createdDate: string;
}

/** Full detail of one executed request in a run (Results tab). */
export interface IRunResponseDetail {
  requestId: number;
  title: string;
  method: string;
  endpoint: string;
  statusCode: number | null;
  durationMs: number | null;
  responseSize: number | null;
  status: 'Passed' | 'Warning' | 'Failed';
  errorMessage: string | null;
  createdDate: string;
  /** Canonical (ignore-applied) response body. */
  responseJson: string | null;
  /** Response headers as a JSON object (name → value). */
  responseHeadersJson: string | null;
  /** Request headers/query/body that were sent (from the ApiRequest definition). */
  requestHeadersJson: string | null;
  requestQueryJson: string | null;
  requestBody: string | null;
}

/** One executed request for the run Excel export (API, input, full response, timing). */
export interface IRunResponseExportRow {
  requestId: number;
  apiName: string;
  method: string;
  endpoint: string;
  title: string;
  requestQueryJson: string | null;
  requestBody: string | null;
  responseJson: string | null;
  statusCode: number | null;
  durationMs: number | null;
  errorMessage: string | null;
}

/** Payload to create a new run. Empty `apiIds` = run all of the dataset's APIs. */
export interface IRunCreate {
  /** The dataset whose requests this run executes. */
  datasetId: number;
  title: string;
  runType: string;
  environment: string;
  description: string | null;
  /** Max requests per API; null = run all of each API's requests. */
  requestLimitPerApi: number | null;
  apiIds: number[];
  /** Parallelism: APIs run concurrently (1 = sequential). */
  maxParallelApis: number;
  /** Parallelism: requests within an API run concurrently (1 = sequential). Product with the above ≤ 20. */
  maxParallelRequestsPerApi: number;
}

/** Editable fields of a pending run (prefills the edit form). Mirrors {@link IRunCreate} + id. */
export interface IRunEdit {
  id: number;
  datasetId: number;
  title: string;
  runType: string;
  environment: string;
  description: string | null;
  requestLimitPerApi: number | null;
  apiIds: number[];
  maxParallelApis: number;
  maxParallelRequestsPerApi: number;
}

/**
 * Live-progress snapshot for one run (from the progress poll endpoint). While Running,
 * executedRequest/totalRequest/progressPercent are live; once finished, status flips and the
 * final counts + duration are populated. Merged into the matching {@link IRun} row.
 */
export interface IRunProgress {
  id: number;
  status: RunStatus;
  totalRequest: number | null;
  executedRequest: number | null;
  passCount: number | null;
  warningCount: number | null;
  failedCount: number | null;
  durationMs: number | null;
  progressPercent: number | null;
}

/** A regression run row. */
export interface IRun {
  id: number;
  title: string;
  description: string | null;
  environment: string;
  runType: string;
  status: RunStatus;
  /** Max requests per API; null = all. */
  requestLimitPerApi?: number | null;
  /** Execution-health counts, set when the run completes (null while pending/running). */
  passCount?: number | null;
  warningCount?: number | null;
  failedCount?: number | null;
  /** Live execution progress % while Running (from server's in-memory tracker); null otherwise. */
  progressPercent?: number | null;
  /** Requests executed so far while Running; null otherwise. */
  executedRequest?: number | null;
  totalRequest: number | null;
  durationMs: number | null;
  createdDate: string;
  /** True if this run is used in a comparison — then it can't be deleted (delete button hidden). */
  hasCompare?: boolean;
}
