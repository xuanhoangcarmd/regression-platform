import { Injectable } from '@angular/core';
import { jsPDF } from 'jspdf';
import autoTable from 'jspdf-autotable';

import { IReportAiAnalysis } from '../models/report-ai.model';
import { IReportDetail } from '../models/report.model';
import { displayReportId } from './report.service';

/** Extra context (not on the report itself) needed to render the PDF. */
export interface IReportPdfContext {
  projectName: string;
}

/** Which optional sections to render (driven by the Export modal). */
export interface IReportPdfOptions {
  includeCharts?: boolean;
  includeDetailed?: boolean;
}

type RGB = [number, number, number];

/** RGB colours used across the document (aligned with the on-screen UI). */
const C = {
  ink: [15, 23, 42] as RGB,
  muted: [100, 116, 139] as RGB,
  line: [226, 232, 240] as RGB,
  green: [22, 163, 74] as RGB,
  amber: [217, 119, 6] as RGB,
  amberFill: [251, 191, 36] as RGB,
  greenFill: [34, 197, 94] as RGB,
  violet: [124, 58, 237] as RGB,
  violetFill: [139, 92, 246] as RGB,
  red: [185, 28, 28] as RGB,
  redFill: [239, 68, 68] as RGB,
  darkRed: [153, 27, 27] as RGB,
};

interface StatCard {
  label: string;
  value: string;
  percent: string | null;
  color: RGB;
}

interface Slice {
  label: string;
  count: number;
  fill: RGB;
  text: RGB;
}

/**
 * Builds a printable PDF of a regression report entirely on the client (jsPDF), laid out to
 * mirror the on-screen Summary tab: coloured stat cards, a Change-Distribution donut, and the
 * Top-APIs-by-change% table. Returns a blob object URL — embed it in an iframe to preview, or
 * use it as an anchor href to download. The caller owns revoking the URL.
 */
@Injectable({ providedIn: 'root' })
export class ReportPdfService {
  fileName(report: IReportDetail): string {
    return `${displayReportId(report.id)}.pdf`;
  }

  buildObjectUrl(
    report: IReportDetail,
    context: IReportPdfContext,
    options?: IReportPdfOptions,
    ai?: IReportAiAnalysis | null,
  ): string {
    const includeCharts = options?.includeCharts ?? true;
    const includeDetailed = options?.includeDetailed ?? true;

    const doc = new jsPDF({ unit: 'pt', format: 'a4' });
    const pageWidth = doc.internal.pageSize.getWidth();
    const margin = 40;
    const contentWidth = pageWidth - margin * 2;
    let y = 48;

    // ---- Title block ----
    doc.setFont('helvetica', 'bold').setFontSize(20).setTextColor(...C.ink);
    doc.text('Regression Report', margin, y);
    doc.setFont('helvetica', 'bold').setFontSize(12).setTextColor(...C.muted);
    doc.text(displayReportId(report.id), margin, y + 18);

    doc.setFont('helvetica', 'normal').setFontSize(10).setTextColor(...C.muted);
    doc.text(`Generated: ${this.formatDate(report.generatedDate)}`, pageWidth - margin, y, { align: 'right' });
    doc.text(`Project: ${context.projectName}`, pageWidth - margin, y + 15, { align: 'right' });

    y += 38;
    // Draw the runs with a vector arrow between them (the standard PDF font has no U+2192 glyph).
    doc.setFont('helvetica', 'normal').setFontSize(11).setTextColor(...C.ink);
    doc.text(report.runATitle, margin, y);
    const arrowX = margin + doc.getTextWidth(report.runATitle) + 10;
    this.drawArrow(doc, arrowX, y - 3, 16);
    doc.setTextColor(...C.ink);
    doc.text(report.runBTitle, arrowX + 26, y);

    y += 14;
    doc.setDrawColor(...C.line).setLineWidth(1).line(margin, y, pageWidth - margin, y);
    y += 22;

    // ---- Executive summary ----
    doc.setFont('helvetica', 'bold').setFontSize(12).setTextColor(...C.ink);
    doc.text('Executive Summary', margin, y);
    y += 15;

    const band = this.scoreBand(report.regressionScore);
    const summary =
      `Regression score ${this.formatScore(report.regressionScore)}% (${band.label}). ` +
      `${this.n(report.unchangedCount)} of ${this.n(report.totalRequest)} requests unchanged across ${this.n(report.totalApis)} APIs. ` +
      (report.breakingApiCount > 0
        ? `${this.n(report.breakingApiCount)} API(s) contain breaking changes.`
        : 'No breaking changes detected.');
    doc.setFont('helvetica', 'normal').setFontSize(10).setTextColor(...C.muted);
    const wrapped = doc.splitTextToSize(summary, contentWidth);
    doc.text(wrapped, margin, y);
    y += wrapped.length * 13 + 14;

    // ---- Stat cards ----
    const cards: StatCard[] = [
      { label: 'Score', value: `${this.formatScore(report.regressionScore)}%`, percent: band.label, color: band.color },
      { label: 'Total', value: this.n(report.totalRequest), percent: null, color: C.ink },
      { label: 'Unchanged', value: this.n(report.unchangedCount), percent: this.pct(report.unchangedCount, report.totalRequest), color: C.green },
      { label: 'Changed', value: this.n(report.changedCount), percent: this.pct(report.changedCount, report.totalRequest), color: C.amber },
      { label: 'Added', value: this.n(report.addedCount), percent: this.pct(report.addedCount, report.totalRequest), color: C.violet },
      { label: 'Removed', value: this.n(report.removedCount), percent: this.pct(report.removedCount, report.totalRequest), color: C.red },
      { label: 'Failed', value: this.n(report.failedCount), percent: this.pct(report.failedCount, report.totalRequest), color: C.darkRed },
    ];
    y = this.drawStatCards(doc, margin, y, contentWidth, cards) + 24;

    // ---- Change Distribution (donut) | Top APIs (table) ----
    if (includeCharts || includeDetailed) {
      const tableX = includeCharts ? margin + 210 : margin; // full width when there is no donut
      doc.setFont('helvetica', 'bold').setFontSize(12).setTextColor(...C.ink);
      if (includeCharts) {
        doc.text('Change Distribution', margin, y);
      }
      if (includeDetailed) {
        doc.text('TOP CHANGED APIS', tableX, y);
      }
      const sectionTop = y + 8;

      let donutBottom = sectionTop;
      if (includeCharts) {
        const slices: Slice[] = [
          { label: 'Unchanged', count: report.unchangedCount, fill: C.greenFill, text: C.green },
          { label: 'Changed', count: report.changedCount, fill: C.amberFill, text: C.amber },
          { label: 'Added', count: report.addedCount, fill: C.violetFill, text: C.violet },
          { label: 'Removed', count: report.removedCount, fill: C.redFill, text: C.red },
          { label: 'Failed', count: report.failedCount, fill: C.darkRed, text: C.darkRed },
        ];
        donutBottom = this.drawDistribution(doc, margin, sectionTop, slices, report.totalRequest);
      }

      let tableBottom = sectionTop;
      if (includeDetailed) {
        const topChanged = [...report.apis]
          .filter((api) => api.changedCount > 0)
          .sort((a, b) => b.changedCount - a.changedCount)
          .slice(0, 5);
        const sumChanged = topChanged.reduce((sum, api) => sum + api.changedCount, 0);

        autoTable(doc, {
          startY: sectionTop + 4,
          margin: { left: tableX, right: margin },
          theme: 'striped',
          head: [['API', 'Changed', '% of Total']],
          body: topChanged.map((api) => [
            `${api.method} ${api.apiName}`,
            this.n(api.changedCount),
            this.pct(api.changedCount, report.totalRequest),
          ]),
          foot: [['Total', this.n(sumChanged), this.pct(sumChanged, report.totalRequest)]],
          styles: { fontSize: 8, cellPadding: 4, textColor: C.ink, overflow: 'linebreak' },
          headStyles: { fillColor: [241, 245, 249], textColor: C.muted, fontStyle: 'bold' },
          footStyles: { fillColor: [248, 250, 252], textColor: C.ink, fontStyle: 'bold' },
          columnStyles: { 1: { halign: 'right' }, 2: { halign: 'right' } },
          // Keep the Total row's numeric cells right-aligned like the body.
          didParseCell: (data) => {
            if (data.section === 'foot' && data.column.index >= 1) {
              data.cell.styles.halign = 'right';
            }
          },
        });
        tableBottom = this.afterTableY(doc);
      }

      y = Math.max(donutBottom, tableBottom) + 26;
    }

    // ---- API change breakdown (all APIs, part of the detailed data) ----
    if (includeDetailed) {
      const rows = [...report.apis]
        .map((api) => {
          const changes = api.changedCount + api.addedCount + api.removedCount;
          const changePercent = api.requestCount > 0 ? Math.round((changes / api.requestCount) * 100) : 0;
          const risk = api.breakingCount > 0 ? 'High' : changes > 0 ? 'Medium' : 'Low';
          return { api, changePercent, risk };
        })
        .sort((a, b) => b.changePercent - a.changePercent);

      doc.setFont('helvetica', 'bold').setFontSize(12).setTextColor(...C.ink);
      doc.text('API Change Breakdown', margin, y);
      y += 6;
      autoTable(doc, {
        startY: y + 6,
        margin: { left: margin, right: margin },
        theme: 'striped',
        head: [['API', 'Endpoint', 'Req', 'Unchg', 'Chg', 'Add', 'Rem', 'Fail', 'Chg %', 'Risk']],
        body: rows.map((row) => [
          `${row.api.method} ${row.api.apiName}`,
          row.api.endpoint,
          this.n(row.api.requestCount),
          this.n(row.api.unchangedCount),
          this.n(row.api.changedCount),
          this.n(row.api.addedCount),
          this.n(row.api.removedCount),
          this.n(row.api.failedCount),
          `${row.changePercent}%`,
          row.risk,
        ]),
        styles: { fontSize: 7, cellPadding: 3, textColor: C.ink, overflow: 'linebreak' },
        headStyles: { fillColor: [241, 245, 249], textColor: C.muted, fontStyle: 'bold' },
        columnStyles: {
          0: { cellWidth: 96 },
          1: { cellWidth: 96, textColor: C.muted },
          2: { halign: 'right' },
          3: { halign: 'right' },
          4: { halign: 'right' },
          5: { halign: 'right' },
          6: { halign: 'right' },
          7: { halign: 'right' },
          8: { halign: 'right' },
          9: { halign: 'center' },
        },
        didParseCell: (data) => {
          // Centre the numeric/Risk column headers (body numbers stay right-aligned).
          if (data.section === 'head' && data.column.index >= 2) {
            data.cell.styles.halign = 'center';
          }
          // Bold the Change % values.
          if (data.section === 'body' && data.column.index === 8) {
            data.cell.styles.fontStyle = 'bold';
          }
        },
      });
      y = this.afterTableY(doc);
    }

    // ---- AI Analysis (own page, from the stored analysis) ----
    if (ai) {
      this.drawAiSection(doc, ai, margin, contentWidth, pageWidth);
    }

    this.drawFooter(doc, report, context, pageWidth, margin);
    return doc.output('bloburl').toString();
  }

  /** Renders the stored AI analysis on its own page: summary, risk, findings, recommendations. */
  private drawAiSection(doc: jsPDF, ai: IReportAiAnalysis, margin: number, contentWidth: number, pageWidth: number): void {
    doc.addPage();
    let y = 48;

    doc.setFont('helvetica', 'bold').setFontSize(16).setTextColor(...C.ink);
    doc.text('AI Analysis', margin, y);
    y += 15;
    doc.setFont('helvetica', 'normal').setFontSize(9).setTextColor(...C.muted);
    const provenance =
      `Analyzed & summarized by AI` + (ai.generatedAt ? `  ·  ${this.formatDate(ai.generatedAt)}` : '');
    doc.text(provenance, margin, y);
    y += 12;
    doc.setDrawColor(...C.line).setLineWidth(1).line(margin, y, pageWidth - margin, y);
    y += 20;

    // Risk line.
    doc.setFont('helvetica', 'bold').setFontSize(10).setTextColor(...C.ink);
    doc.text(`Risk: ${ai.riskLevel}   ·   Score ${ai.riskScore}/100`, margin, y);
    y += 18;

    // Executive summary.
    doc.setFont('helvetica', 'bold').setFontSize(12).setTextColor(...C.ink);
    doc.text('Executive Summary', margin, y);
    y += 14;
    doc.setFont('helvetica', 'normal').setFontSize(10).setTextColor(...C.muted);
    const wrapped = doc.splitTextToSize(ai.summary, contentWidth);
    doc.text(wrapped, margin, y);
    y += wrapped.length * 13 + 14;

    // Recommendation.
    if (ai.recommendation) {
      doc.setFont('helvetica', 'bold').setFontSize(11).setTextColor(...C.ink);
      doc.text('Recommendation', margin, y);
      y += 13;
      doc.setFont('helvetica', 'normal').setFontSize(10).setTextColor(...C.muted);
      const recWrapped = doc.splitTextToSize(ai.recommendation, contentWidth);
      doc.text(recWrapped, margin, y);
      y += recWrapped.length * 13 + 12;
    }

    // Bullet-list sections (autoTable handles page breaks).
    y = this.drawBulletTable(doc, y, margin, 'Key Findings', ai.findings);
    y = this.drawBulletTable(doc, y, margin, 'Recommendations', ai.recommendations);
    this.drawBulletTable(doc, y, margin, 'Risk Factors', ai.riskFactors);
  }

  /** Draws a titled bullet list via autoTable; returns the Y after the table. */
  private drawBulletTable(doc: jsPDF, startY: number, margin: number, title: string, items: string[]): number {
    if (!items.length) {
      return startY;
    }
    doc.setFont('helvetica', 'bold').setFontSize(11).setTextColor(...C.ink);
    doc.text(title, margin, startY);
    autoTable(doc, {
      startY: startY + 6,
      margin: { left: margin, right: margin },
      theme: 'plain',
      body: items.map((item) => ['•', item]),
      styles: { fontSize: 9, cellPadding: 3, textColor: C.ink, overflow: 'linebreak' },
      columnStyles: { 0: { cellWidth: 12, textColor: C.muted }, 1: { cellWidth: 'auto' } },
    });
    return this.afterTableY(doc) + 16;
  }

  /** Draws a small right-pointing arrow (shaft + filled head) centred vertically at cy. */
  private drawArrow(doc: jsPDF, x: number, cy: number, len: number): void {
    doc.setDrawColor(...C.muted).setLineWidth(1.2);
    doc.line(x, cy, x + len - 5, cy);
    doc.setFillColor(...C.muted);
    doc.triangle(x + len, cy, x + len - 6, cy - 3.5, x + len - 6, cy + 3.5, 'F');
  }

  /** Draws a row of equal-width coloured stat cards; returns the bottom Y. */
  private drawStatCards(doc: jsPDF, x0: number, y: number, width: number, cards: StatCard[]): number {
    const gap = 6;
    const cardW = (width - gap * (cards.length - 1)) / cards.length;
    const cardH = 50;
    cards.forEach((card, i) => {
      const x = x0 + i * (cardW + gap);
      const cx = x + cardW / 2;
      doc.setFillColor(255, 255, 255).setDrawColor(...C.line).setLineWidth(0.8);
      doc.roundedRect(x, y, cardW, cardH, 4, 4, 'FD');
      doc.setFont('helvetica', 'normal').setFontSize(6).setTextColor(...C.muted);
      doc.text(card.label.toUpperCase(), cx, y + 13, { align: 'center' });
      doc.setFont('helvetica', 'bold').setFontSize(12).setTextColor(...card.color);
      doc.text(card.value, cx, y + 30, { align: 'center' });
      if (card.percent) {
        doc.setFont('helvetica', 'normal').setFontSize(6.5).setTextColor(...card.color);
        doc.text(card.percent, cx, y + 42, { align: 'center' });
      }
    });
    return y + cardH;
  }

  /** Draws the donut + legend; returns the bottom Y of the block. */
  private drawDistribution(doc: jsPDF, x0: number, top: number, slices: Slice[], total: number): number {
    const cx = x0 + 52;
    const cy = top + 56;
    const outerR = 46;
    const innerR = 27;

    if (total > 0) {
      let start = -Math.PI / 2;
      for (const slice of slices) {
        if (slice.count <= 0) {
          continue;
        }
        const sweep = (slice.count / total) * Math.PI * 2;
        const steps = Math.max(2, Math.ceil((sweep / (Math.PI * 2)) * 64));
        const points: [number, number][] = [[cx, cy]];
        for (let i = 0; i <= steps; i++) {
          const a = start + sweep * (i / steps);
          points.push([cx + Math.cos(a) * outerR, cy + Math.sin(a) * outerR]);
        }
        const deltas: [number, number][] = [];
        for (let i = 1; i < points.length; i++) {
          deltas.push([points[i][0] - points[i - 1][0], points[i][1] - points[i - 1][1]]);
        }
        doc.setFillColor(...slice.fill);
        doc.lines(deltas, points[0][0], points[0][1], [1, 1], 'F', true);
        start += sweep;
      }
      // Donut hole + centre total.
      doc.setFillColor(255, 255, 255).circle(cx, cy, innerR, 'F');
      doc.setFont('helvetica', 'bold').setFontSize(11).setTextColor(...C.ink);
      doc.text(this.n(total), cx, cy - 1, { align: 'center' });
      doc.setFont('helvetica', 'normal').setFontSize(6).setTextColor(...C.muted);
      doc.text('TOTAL', cx, cy + 9, { align: 'center' });
    } else {
      doc.setDrawColor(...C.line).circle(cx, cy, outerR, 'S');
    }

    // Legend (to the right of the donut).
    let ly = top + 20;
    const lx = cx + outerR + 12;
    for (const slice of slices) {
      if (slice.count <= 0) {
        continue;
      }
      doc.setFillColor(...slice.fill).roundedRect(lx, ly - 6, 7, 7, 1.5, 1.5, 'F');
      doc.setFont('helvetica', 'normal').setFontSize(7).setTextColor(...C.ink);
      doc.text(slice.label, lx + 12, ly);
      doc.setTextColor(...slice.text);
      doc.text(`${this.n(slice.count)} (${this.pct(slice.count, total)})`, lx + 12, ly + 9);
      ly += 22;
    }

    return Math.max(cy + outerR, ly);
  }

  private drawFooter(doc: jsPDF, report: IReportDetail, context: IReportPdfContext, pageWidth: number, margin: number): void {
    const pages = doc.getNumberOfPages();
    const bottom = doc.internal.pageSize.getHeight() - 20;
    for (let page = 1; page <= pages; page++) {
      doc.setPage(page);
      doc.setFont('helvetica', 'normal').setFontSize(8).setTextColor(...C.muted);
      doc.text(`${displayReportId(report.id)} · ${context.projectName}`, margin, bottom);
      doc.text(`Page ${page} / ${pages}`, pageWidth - margin, bottom, { align: 'right' });
    }
  }

  private afterTableY(doc: jsPDF): number {
    return (doc as unknown as { lastAutoTable: { finalY: number } }).lastAutoTable.finalY;
  }

  private scoreBand(score: number): { label: string; color: RGB } {
    if (score >= 95) return { label: 'Excellent', color: C.green };
    if (score >= 85) return { label: 'Good', color: C.green };
    if (score >= 70) return { label: 'Warning', color: C.amber };
    return { label: 'Critical', color: C.red };
  }

  private n(value: number): string {
    return value.toLocaleString('en-US');
  }

  private pct(count: number, total: number): string {
    return total > 0 ? `${Math.round((count / total) * 100)}%` : '0%';
  }

  private formatScore(score: number): string {
    return (Math.round(score * 10) / 10).toString();
  }

  private formatDate(iso: string): string {
    const date = new Date(iso);
    return Number.isNaN(date.getTime()) ? iso : date.toLocaleString('en-GB');
  }
}
