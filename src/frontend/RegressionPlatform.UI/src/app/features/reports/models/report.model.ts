/** Lifecycle status of a report (mirrors Report.Status). More statuses arrive with async AI phases. */
export type ReportStatus = 'Completed';

export type ReportStatusFilter = 'all' | ReportStatus;

/** Risk level of a report (mirrors Report.RiskLevel). */
export type ReportRiskLevel = 'Low' | 'Medium' | 'High' | 'Critical';

/** A report row for the Reports list (report snapshot + its compare's run labels). */
export interface IReportListItem {
  id: number;
  compareJobId: number;
  runAId: number;
  runBId: number;
  runATitle: string;
  runBTitle: string;
  name: string;
  status: ReportStatus;
  regressionScore: number;
  riskLevel: ReportRiskLevel;
  totalRequest: number;
  unchangedCount: number;
  changedCount: number;
  addedCount: number;
  removedCount: number;
  failedCount: number;
  totalApis: number;
  breakingApiCount: number;
  generatedDate: string;
  createdDate: string;
}

/** Per-API breakdown for a report (compare stats + a breaking-change count for the API). */
export interface IReportApiStat {
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
  breakingCount: number;
}

/** Report detail (Summary tab): the snapshot header + the compare's per-API breakdown. */
export interface IReportDetail {
  id: number;
  compareJobId: number;
  runAId: number;
  runBId: number;
  runATitle: string;
  runBTitle: string;
  name: string;
  status: ReportStatus;
  regressionScore: number;
  riskLevel: ReportRiskLevel;
  totalRequest: number;
  unchangedCount: number;
  changedCount: number;
  addedCount: number;
  removedCount: number;
  failedCount: number;
  totalApis: number;
  breakingApiCount: number;
  generatedDate: string;
  createdDate: string;
  apis: IReportApiStat[];
}

/** Payload to generate (or regenerate) a report from a Completed compare. */
export interface IReportGenerate {
  compareId: number;
}

/** Response time of one request in each compared run (paired by request). */
export interface IReportRequestTiming {
  requestId: number;
  apiId: number;
  apiName: string;
  title: string;
  method: string;
  endpoint: string;
  durationMsA: number | null;
  durationMsB: number | null;
}

/** Average response time of one API in each compared run. */
export interface IReportApiTiming {
  apiId: number;
  apiName: string;
  method: string;
  avgDurationMsA: number | null;
  avgDurationMsB: number | null;
}

/** Response-time comparison (Run A vs Run B) driving the Distribution tab charts. */
export interface IReportPerformance {
  runAId: number;
  runBId: number;
  runATitle: string;
  runBTitle: string;
  perRequest: IReportRequestTiming[];
  perApi: IReportApiTiming[];
}

/** One latency-histogram bucket (request counts per run). */
export interface IReportPerfBin {
  fromMs: number;
  toMs: number;
  countA: number;
  countB: number;
}

export interface IReportPerfHistogram {
  binSizeMs: number;
  bins: IReportPerfBin[];
}

/** Per-API latency + size stats for the Distribution tab table (p95/max/size + avg for Δ%). */
export interface IReportPerfApiStat {
  apiId: number;
  apiName: string;
  method: string;
  p95MsA: number | null;
  p95MsB: number | null;
  maxMsA: number | null;
  maxMsB: number | null;
  minMsA: number | null;
  minMsB: number | null;
  avgSizeA: number | null;
  avgSizeB: number | null;
  avgMsA: number | null;
  avgMsB: number | null;
}

/** Extra performance statistics for the Distribution tab (latency histogram + per-API table). */
export interface IReportPerformanceStats {
  runATitle: string;
  runBTitle: string;
  histogram: IReportPerfHistogram;
  perApi: IReportPerfApiStat[];
}
