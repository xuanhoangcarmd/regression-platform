import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { catchError, of } from 'rxjs';

import { ModalComponent } from '@shared/components';

import { IReportAiAnalysis, IReportAiQaTurn } from '../../models/report-ai.model';
import { IReportDetail, ReportRiskLevel } from '../../models/report.model';
import { ReportAiConversationStore } from '../../services/report-ai-conversation.store';
import { ReportAiService } from '../../services/report-ai.service';

/** AI Analysis tab: Executive Summary, Risk Assessment, Recommendations and an Ask-AI box. */
@Component({
  selector: 'app-report-ai-analysis',
  standalone: true,
  imports: [DatePipe, ReactiveFormsModule, MatIconModule, ModalComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './report-ai-analysis.component.html',
})
export class ReportAiAnalysisComponent {
  readonly report = input.required<IReportDetail>();

  private readonly aiService = inject(ReportAiService);
  private readonly conversationStore = inject(ReportAiConversationStore);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly regenerating = signal(false);
  protected readonly confirmOpen = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly analysis = signal<IReportAiAnalysis | null>(null);

  /** True when the deterministic rule-based fallback produced the analysis (no LLM configured). */
  protected readonly isSample = computed(() => this.analysis()?.provider === 'RuleBased');

  // ----- Ask AI about this report (ephemeral, single-shot; disabled when rule-based) -----

  /** The Ask-AI input. */
  protected readonly questionControl = new FormControl<string>('', { nonNullable: true });
  /** Ephemeral Q&A thread for this session (newest last); never persisted. */
  protected readonly qaTurns = signal<IReportAiQaTurn[]>([]);
  /** True while a question is in flight (one at a time). */
  protected readonly asking = signal(false);
  /** Ask is available only once a real AI provider produced the analysis. */
  protected readonly askDisabled = computed(() => this.isSample() || this.loading() || this.errorMessage() !== null);

  constructor() {
    queueMicrotask(() => {
      // Restore any earlier conversation for this report (survives tab switches / reloads this session).
      this.qaTurns.set(this.conversationStore.get(this.report().id));
      this.load();
    });
  }

  private load(): void {
    this.aiService
      .getAnalysis(this.report().id)
      .pipe(
        catchError(() => of(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((analysis) => {
        this.loading.set(false);
        if (analysis === null) {
          this.errorMessage.set('Failed to run the AI analysis.');
          return;
        }
        this.analysis.set(analysis);
      });
  }

  /** Confirm dialog → run the regeneration. */
  protected confirmRegenerate(): void {
    this.confirmOpen.set(false);
    this.regenerate();
  }

  /** Re-run the AI provider on demand (Regenerate button) and persist the fresh result. */
  private regenerate(): void {
    if (this.regenerating()) {
      return;
    }
    this.regenerating.set(true);
    this.aiService
      .regenerate(this.report().id)
      .pipe(
        catchError(() => of(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((analysis) => {
        this.regenerating.set(false);
        if (analysis !== null) {
          this.analysis.set(analysis);
        }
      });
  }

  /** Submit the typed question. */
  protected submitQuestion(): void {
    this.ask(this.questionControl.value);
  }

  /** Ask one of the suggested example questions. */
  protected askExample(question: string): void {
    this.ask(question);
  }

  /** Clear the conversation for this report (in this session). */
  protected clearConversation(): void {
    if (this.asking()) {
      return;
    }
    this.qaTurns.set([]);
    this.conversationStore.clear(this.report().id);
  }

  /**
   * Ask a single question about this report. Appends a pending turn, then fills the answer in place.
   * One question at a time; input is cleared on submit. Ephemeral — nothing is persisted.
   */
  private ask(raw: string): void {
    const question = raw.trim();
    if (!question || this.asking() || this.askDisabled()) {
      return;
    }

    this.asking.set(true);
    this.questionControl.reset('');
    const index = this.qaTurns().length;
    this.qaTurns.update((turns) => [...turns, { question, answer: null, pending: true, failed: false }]);

    this.aiService
      .ask(this.report().id, question)
      .pipe(
        catchError(() => of(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.asking.set(false);
        this.qaTurns.update((turns) =>
          turns.map((turn, i) =>
            i === index
              ? {
                  ...turn,
                  pending: false,
                  failed: result === null,
                  answer: result?.answer ?? 'Sorry — something went wrong. Please try again.',
                }
              : turn,
          ),
        );
        // Persist the completed turn so it survives tab switches / reloads this session.
        this.conversationStore.set(this.report().id, this.qaTurns());
      });
  }

  /** Colour classes for the risk badge. */
  protected riskBadgeClass(risk: ReportRiskLevel): string {
    switch (risk) {
      case 'Low':
        return 'bg-green-50 text-green-700';
      case 'Medium':
        return 'bg-amber-50 text-amber-700';
      case 'High':
        return 'bg-orange-50 text-orange-700';
      case 'Critical':
        return 'bg-red-50 text-red-700';
    }
  }
}
