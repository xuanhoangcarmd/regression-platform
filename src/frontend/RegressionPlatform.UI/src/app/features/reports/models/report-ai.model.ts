import { ReportRiskLevel } from './report.model';

/**
 * AI analysis of a report (Executive Summary / Risk Assessment / Recommendations / Ask-AI).
 * This is the shape the server-side AI endpoint (DeepSeek-backed) will return; for now the UI
 * is driven by a locally-built sample (see {@link ReportAiService}).
 */
export interface IReportAiAnalysis {
  /** Natural-language executive summary paragraph. */
  summary: string;
  /** Key findings, rendered as a green checklist. */
  findings: string[];
  /** Highlighted recommendation callout under the summary. */
  recommendation: string;

  /** Overall risk band (mirrors the report's risk level). */
  riskLevel: ReportRiskLevel;
  /** Risk score 0–100 (higher = riskier; distinct from the regression score). */
  riskScore: number;
  /** Key risk factors, rendered with warning icons. */
  riskFactors: string[];

  /** Actionable AI recommendations. */
  recommendations: string[];
  /** Suggested prompts for the "Ask AI" box. */
  exampleQuestions: string[];

  /** Which provider produced this analysis (e.g. "DeepSeek", "RuleBased"). */
  provider: string;
  /** When this analysis was generated/persisted (ISO string), null if not stored. */
  generatedAt: string | null;
}

/** Backend reply to an "Ask AI about this report" question. */
export interface IReportAiAskResponse {
  /** The AI's answer (or a scoped refusal for off-topic questions). */
  answer: string;
  /** Which provider produced the answer (e.g. "DeepSeek", "RuleBased"). */
  provider: string;
}

/** One ephemeral Q&A turn shown in the Ask-AI thread (not persisted). */
export interface IReportAiQaTurn {
  question: string;
  /** null while the answer is being fetched. */
  answer: string | null;
  /** true while awaiting the backend. */
  pending: boolean;
  /** true if the request failed. */
  failed: boolean;
}
