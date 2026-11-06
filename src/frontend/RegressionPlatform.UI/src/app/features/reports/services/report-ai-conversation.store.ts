import { Injectable } from '@angular/core';

import { IReportAiQaTurn } from '../models/report-ai.model';

/**
 * Holds the ephemeral "Ask AI about this report" conversation per report so it survives switching
 * tabs (the AI tab component is destroyed on tab change) and page reloads within the same browser
 * tab. Mirrored to sessionStorage — session-scoped by design (cleared when the tab closes); nothing
 * is persisted server-side. Pending (in-flight) turns are never stored.
 */
@Injectable({ providedIn: 'root' })
export class ReportAiConversationStore {
  private static readonly storageKey = 'report-ai-conversations';

  private readonly cache = new Map<number, IReportAiQaTurn[]>();

  constructor() {
    this.restore();
  }

  /** The stored turns for a report (empty if none). */
  get(reportId: number): IReportAiQaTurn[] {
    return this.cache.get(reportId) ?? [];
  }

  /** Replace the stored turns for a report (only completed turns are persisted). */
  set(reportId: number, turns: IReportAiQaTurn[]): void {
    this.cache.set(reportId, turns);
    this.persist();
  }

  /** Forget a report's conversation (the "Clear" action). */
  clear(reportId: number): void {
    this.cache.delete(reportId);
    this.persist();
  }

  private restore(): void {
    try {
      const raw = sessionStorage.getItem(ReportAiConversationStore.storageKey);
      if (!raw) {
        return;
      }
      const parsed = JSON.parse(raw) as Record<string, IReportAiQaTurn[]>;
      for (const [id, turns] of Object.entries(parsed)) {
        // Drop any turn still pending when the tab was reloaded — it can never resolve.
        this.cache.set(Number(id), turns.filter((turn) => !turn.pending));
      }
    } catch {
      // Corrupt/unavailable storage — start empty.
    }
  }

  private persist(): void {
    try {
      const snapshot: Record<number, IReportAiQaTurn[]> = {};
      for (const [id, turns] of this.cache) {
        const completed = turns.filter((turn) => !turn.pending);
        if (completed.length > 0) {
          snapshot[id] = completed;
        }
      }
      sessionStorage.setItem(ReportAiConversationStore.storageKey, JSON.stringify(snapshot));
    } catch {
      // Storage full/unavailable — in-memory copy still works for this session.
    }
  }
}
