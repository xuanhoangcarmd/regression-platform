import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

import { ModalComponent } from '@shared/components';

import { IReportDetail } from '../../../models/report.model';
import { displayReportId } from '../../../services/report.service';

/** Compose-and-send-report-by-email modal. UI only — sending is not wired up yet (coming soon). */
@Component({
  selector: 'app-report-email-modal',
  standalone: true,
  imports: [MatIconModule, ModalComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './report-email-modal.component.html',
})
export class ReportEmailModalComponent {
  readonly report = input.required<IReportDetail>();
  readonly projectName = input<string>('');
  readonly closed = output<void>();

  protected readonly displayReportId = displayReportId;

  /** Recipient chips — locally editable; not submitted anywhere yet. */
  protected readonly recipients = signal<string[]>([]);
  protected readonly draftRecipient = signal('');

  protected readonly subject = computed(
    () =>
      `[${this.projectName()}] Regression Report ${displayReportId(this.report().id)} — ` +
      `${this.report().runATitle} vs ${this.report().runBTitle}`,
  );

  protected readonly message = computed(() => {
    const report = this.report();
    const changed = report.changedCount + report.addedCount + report.removedCount + report.failedCount;
    const total = Math.max(report.totalRequest, 1);
    const changedPct = Math.round((changed / total) * 100);
    return (
      `Hello team,\n\n` +
      `Please find attached the regression report for ${report.runATitle} vs ${report.runBTitle}.\n\n` +
      `Summary:\n` +
      `- Regression Score: ${this.round1(report.regressionScore)}% (${this.scoreBand(report.regressionScore)})\n` +
      `- Total Requests: ${report.totalRequest.toLocaleString()}\n` +
      `- Changed Requests: ${changed.toLocaleString()} (${changedPct}%)\n` +
      `- Breaking Changes: ${report.breakingApiCount} APIs\n\n` +
      `Please review the breaking changes before deployment.\n\n` +
      `Best regards,\n` +
      `Regression Platform`
    );
  });

  /** Attachment toggles (visual only). */
  protected readonly includePdf = signal(true);
  protected readonly includeExcel = signal(true);
  protected readonly includeAiSummary = signal(true);
  protected readonly includeCompareLink = signal(false);

  protected addRecipient(): void {
    const value = this.draftRecipient().trim().replace(/,$/, '');
    if (value && !this.recipients().includes(value)) {
      this.recipients.update((list) => [...list, value]);
    }
    this.draftRecipient.set('');
  }

  protected removeRecipient(email: string): void {
    this.recipients.update((list) => list.filter((item) => item !== email));
  }

  private scoreBand(score: number): string {
    if (score >= 95) return 'Excellent';
    if (score >= 85) return 'Good';
    if (score >= 70) return 'Warning';
    return 'Critical';
  }

  private round1(value: number): number {
    return Math.round(value * 10) / 10;
  }
}
