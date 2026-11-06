import { Injectable, inject, signal } from '@angular/core';
import { Borders, Cell, Workbook } from 'exceljs';
import JSZip from 'jszip';
import { firstValueFrom } from 'rxjs';

import { IReportAiAnalysis } from '../models/report-ai.model';
import { IReportApiStat, IReportDetail } from '../models/report.model';
import { ReportAiService } from './report-ai.service';
import { IReportPdfContext, ReportPdfService } from './report-pdf.service';
import { CompareApiService } from '@features/compare/apis/compare-api.service';
import { ICompareChangeSummary, ICompareFullDetail, ICompareTestResultRow } from '@features/compare/models/compare.model';
import { displayCompareId } from '@features/compare/services/compare.service';
import { displayReportId } from './report.service';

export type ReportExportFormat = 'pdf' | 'excel' | 'testresults' | 'livedata' | 'json';

/**
 * How the Test Results export carries request/response bodies: inside the Excel cells (truncated at
 * Excel's limit) or as TestCase_*.txt files next to the workbook (cells hold a link), shipped as a ZIP.
 */
export type TestResultsOutput = 'excel' | 'file';

/** What to include in the export (from the Export modal). */
export interface IReportExportOptions {
  includeCharts: boolean;
  includeDetailed: boolean;
  /** Include the persisted AI summary (never triggers a fresh AI run). */
  includeAiSummary: boolean;
  /** Excel only: add a "Full API" sheet listing every request's fields per row (change type + values). */
  includeFullApi: boolean;
  /**
   * Excel only: add a "Field Report" sheet with ONLY the changes whose root-level field matches
   * {@link fieldReportFields}, grouped by (change type, field) — for focused reports (e.g. LEDStatus/FF/DTCs).
   */
  includeFieldReport: boolean;
  /** Comma-separated root-field names of interest (case-insensitive contains). Empty = all fields. */
  fieldReportFields: string;
  /** Test Results only: bodies in cells ('excel') or as linked .txt files in a ZIP ('file'). */
  testResultsOutput: TestResultsOutput;
  /**
   * Live Data Result only: also split each request's fields (query string / body) into their own
   * columns after the template columns — same layout as the request import Excel.
   */
  liveDataSplitRequest?: boolean;
}

/** Distribution categories with their canvas/Excel colours (hex). */
const DIST = [
  { label: 'Unchanged', key: 'unchangedCount' as const, color: '#22c55e' },
  { label: 'Changed', key: 'changedCount' as const, color: '#fbbf24' },
  { label: 'Added', key: 'addedCount' as const, color: '#8b5cf6' },
  { label: 'Removed', key: 'removedCount' as const, color: '#ef4444' },
  { label: 'Failed', key: 'failedCount' as const, color: '#991b1b' },
];

const HEADER_FILL = 'FFF1F5F9';
const HEADER_TEXT = 'FF475569';
const BORDER = { style: 'thin', color: { argb: 'FFE2E8F0' } } as const;
const BORDER_ALL: Partial<Borders> = { top: BORDER, left: BORDER, bottom: BORDER, right: BORDER };

/**
 * Client-side export of a report to PDF / Excel / CSV / JSON. PDF uses jsPDF, Excel uses ExcelJS
 * (styled workbook + an embedded Change-Distribution donut image), CSV/JSON are built by hand.
 */
@Injectable({ providedIn: 'root' })
export class ReportExportService {
  private readonly pdf = inject(ReportPdfService);
  private readonly compareApi = inject(CompareApiService);
  private readonly aiService = inject(ReportAiService);

  /**
   * Ids of reports whose export is currently generating. Lives in this root singleton, so the progress
   * survives navigation — a report detail page re-opened while its export runs still shows the status.
   */
  readonly exportingReportIds = signal<ReadonlySet<number>>(new Set<number>());

  /** Runs the export, tracking the report as "generating" until the file is ready (or it fails). */
  export(
    format: ReportExportFormat,
    report: IReportDetail,
    context: IReportPdfContext,
    options: IReportExportOptions,
  ): Promise<void> {
    this.setExporting(report.id, true);
    return this.run(format, report, context, options).finally(() => this.setExporting(report.id, false));
  }

  private setExporting(reportId: number, on: boolean): void {
    this.exportingReportIds.update((ids) => {
      const next = new Set(ids);
      if (on) {
        next.add(reportId);
      } else {
        next.delete(reportId);
      }
      return next;
    });
  }

  private async run(
    format: ReportExportFormat,
    report: IReportDetail,
    context: IReportPdfContext,
    options: IReportExportOptions,
  ): Promise<void> {
    // Reuse the already-stored AI analysis only (never triggers a fresh AI run).
    const ai = options.includeAiSummary && format !== 'testresults' && format !== 'livedata'
      ? await firstValueFrom(this.aiService.getStoredAnalysis(report.id))
      : null;

    switch (format) {
      case 'pdf':
        this.exportPdf(report, context, options, ai);
        break;
      case 'excel':
        await this.buildExcel(report, context, options, ai);
        break;
      case 'testresults':
        await this.buildTestResults(report, options.testResultsOutput);
        break;
      case 'livedata':
        await this.buildLiveDataResult(report, context, options.liveDataSplitRequest ?? false);
        break;
      case 'json':
        await this.buildJson(report, context, options, ai);
        break;
    }
  }

  private exportPdf(
    report: IReportDetail,
    context: IReportPdfContext,
    options: IReportExportOptions,
    ai: IReportAiAnalysis | null,
  ): void {
    const url = this.pdf.buildObjectUrl(
      report,
      context,
      { includeCharts: options.includeCharts, includeDetailed: options.includeDetailed },
      ai,
    );
    this.downloadUrl(url, `${displayReportId(report.id)}.pdf`);
    setTimeout(() => URL.revokeObjectURL(url), 4000);
  }

  // ---------------- Excel (ExcelJS) ----------------

  private async buildExcel(
    report: IReportDetail,
    context: IReportPdfContext,
    options: IReportExportOptions,
    ai: IReportAiAnalysis | null,
  ): Promise<void> {
    const workbook = new Workbook();
    workbook.creator = 'Regression Platform';
    workbook.created = new Date();

    this.buildSummarySheet(workbook, report, context, options);
    if (ai) {
      this.buildAiSheet(workbook, ai);
    }
    if (options.includeDetailed) {
      this.buildApisSheet(workbook, report);
      this.buildBreakingSheet(workbook, report);
      const changeSummary = await this.fetchChangeSummary(report.compareJobId);
      if (changeSummary) {
        this.buildChangesSheet(workbook, changeSummary);
      }
    }

    // Optional per-request sheets (both feed off the same full-details fetch). Added on top of the
    // other sheets, which stay unchanged; default off and only offered for Excel.
    if (options.includeFullApi || options.includeFieldReport) {
      const fullDetails = await this.fetchFullDetails(report.compareJobId);
      if (options.includeFullApi) {
        this.buildFullApiSheet(workbook, fullDetails);
      }
      if (options.includeFieldReport) {
        this.buildFieldReportSheet(workbook, fullDetails, options.fieldReportFields);
      }
    }

    const buffer = await workbook.xlsx.writeBuffer();
    const blob = new Blob([buffer], {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    });
    this.downloadBlob(blob, `${displayReportId(report.id)}.xlsx`);
  }

  private buildSummarySheet(
    workbook: Workbook,
    report: IReportDetail,
    context: IReportPdfContext,
    options: IReportExportOptions,
  ): void {
    const ws = workbook.addWorksheet('Summary', { views: [{ showGridLines: false }] });
    ws.getColumn(1).width = 26;
    ws.getColumn(2).width = 22;
    ws.getColumn(3).width = 3;
    ws.getColumn(4).width = 16;
    ws.getColumn(5).width = 12;

    ws.mergeCells('A1:B1');
    const title = ws.getCell('A1');
    title.value = 'Regression Report';
    title.font = { bold: true, size: 15, color: { argb: 'FF0F172A' } };
    ws.getCell('A2').value = `${displayReportId(report.id)}  ·  ${report.runATitle} vs ${report.runBTitle}`;
    ws.getCell('A2').font = { size: 10, color: { argb: HEADER_TEXT } };
    ws.getCell('A3').value = `Project: ${context.projectName}   ·   Generated: ${this.formatDate(report.generatedDate)}`;
    ws.getCell('A3').font = { size: 9, color: { argb: 'FF94A3B8' } };

    // SUMMARY metric/value table.
    ws.getCell('A5').value = 'SUMMARY';
    ws.getCell('A5').font = { bold: true, size: 11, color: { argb: 'FF0F172A' } };
    this.headerRow(ws.getRow(6), ['Metric', 'Value']);
    const metrics: [string, string | number][] = [
      ['Total Requests', report.totalRequest],
      ['Unchanged', report.unchangedCount],
      ['Changed', report.changedCount],
      ['Added', report.addedCount],
      ['Removed', report.removedCount],
      ['Failed', report.failedCount],
      ['Regression Score', `${report.regressionScore}%`],
      ['Risk Level', report.riskLevel],
      ['Deployment Recommendation', this.recommendation(report.riskLevel)],
      ['Total APIs', report.totalApis],
      ['Breaking APIs', report.breakingApiCount],
    ];
    let row = 7;
    for (const [metric, value] of metrics) {
      const r = ws.getRow(row);
      r.getCell(1).value = metric;
      r.getCell(2).value = value;
      if (typeof value === 'number') {
        r.getCell(2).numFmt = '#,##0';
      }
      if (metric === 'Regression Score') {
        r.getCell(2).alignment = { horizontal: 'right' };
      }
      r.getCell(1).border = BORDER_ALL;
      r.getCell(2).border = BORDER_ALL;
      row++;
    }

    // Change-distribution donut image (to the right of the metric table).
    if (options.includeCharts) {
      const dataUrl = this.renderDistributionPng(report);
      if (dataUrl) {
        const base64 = dataUrl.split(',')[1] ?? dataUrl;
        const imageId = workbook.addImage({ base64, extension: 'png' });
        ws.addImage(imageId, { tl: { col: 3, row: 5 }, ext: { width: 340, height: 190 } });
      }
    }

    // TOP CHANGED APIS.
    const topStart = row + 2;
    ws.getCell(`A${topStart}`).value = 'TOP CHANGED APIS';
    ws.getCell(`A${topStart}`).font = { bold: true, size: 11, color: { argb: 'FF0F172A' } };
    this.headerRow(ws.getRow(topStart + 1), ['API', 'Total Requests', 'Changed', 'Added', 'Removed', '% of Total']);

    const topChanged = [...report.apis]
      .filter((api) => api.changedCount > 0)
      .sort((a, b) => b.changedCount - a.changedCount)
      .slice(0, 5);
    let tr = topStart + 2;
    for (const api of topChanged) {
      const r = ws.getRow(tr);
      r.values = [
        `${api.method} ${api.apiName}`,
        api.requestCount,
        api.changedCount,
        api.addedCount,
        api.removedCount,
        this.ratio(api.changedCount, report.totalRequest),
      ];
      [2, 3, 4, 5].forEach((c) => (r.getCell(c).numFmt = '#,##0'));
      r.getCell(6).numFmt = '0.00%';
      r.eachCell((cell) => (cell.border = BORDER_ALL));
      tr++;
    }
    const sumChanged = topChanged.reduce((sum, api) => sum + api.changedCount, 0);
    const totalRow = ws.getRow(tr);
    totalRow.values = ['Total', '', sumChanged, '', '', this.ratio(sumChanged, report.totalRequest)];
    totalRow.getCell(3).numFmt = '#,##0';
    totalRow.getCell(6).numFmt = '0.00%';
    totalRow.eachCell((cell) => {
      cell.font = { bold: true };
      cell.border = BORDER_ALL;
    });
  }

  /** "AI Summary" sheet — the persisted AI analysis (executive summary, risk, findings, recommendations). */
  private buildAiSheet(workbook: Workbook, ai: IReportAiAnalysis): void {
    const ws = workbook.addWorksheet('AI Summary', { views: [{ showGridLines: false }] });
    ws.getColumn(1).width = 110;

    ws.getCell('A1').value = 'AI Analysis';
    ws.getCell('A1').font = { bold: true, size: 15, color: { argb: 'FF0F172A' } };
    const provenance =
      `Analyzed & summarized by AI` +
      (ai.generatedAt ? `   ·   ${this.formatDate(ai.generatedAt)}` : '');
    ws.getCell('A2').value = provenance;
    ws.getCell('A2').font = { size: 9, color: { argb: 'FF94A3B8' } };
    ws.getCell('A3').value = `Risk Level: ${ai.riskLevel}     ·     Risk Score: ${ai.riskScore}/100`;
    ws.getCell('A3').font = { bold: true, size: 10, color: { argb: HEADER_TEXT } };

    let row = 5;

    ws.getCell(`A${row}`).value = 'EXECUTIVE SUMMARY';
    ws.getCell(`A${row}`).font = { bold: true, size: 11, color: { argb: 'FF0F172A' } };
    row++;
    const summaryCell = ws.getCell(`A${row}`);
    summaryCell.value = ai.summary;
    summaryCell.alignment = { wrapText: true, vertical: 'top' };
    summaryCell.font = { size: 10, color: { argb: 'FF0F172A' } };
    ws.getRow(row).height = 48;
    row += 2;

    if (ai.recommendation) {
      ws.getCell(`A${row}`).value = 'RECOMMENDATION';
      ws.getCell(`A${row}`).font = { bold: true, size: 11, color: { argb: 'FF0F172A' } };
      row++;
      const rec = ws.getCell(`A${row}`);
      rec.value = ai.recommendation;
      rec.alignment = { wrapText: true, vertical: 'top' };
      rec.font = { size: 10, color: { argb: 'FF0F172A' } };
      rec.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFEEF2FF' } };
      ws.getRow(row).height = 40;
      row += 2;
    }

    row = this.aiListBlock(ws, row, 'KEY FINDINGS', ai.findings);
    row = this.aiListBlock(ws, row, 'RECOMMENDATIONS', ai.recommendations);
    this.aiListBlock(ws, row, 'RISK FACTORS', ai.riskFactors);
  }

  /** Writes a titled bullet list on the AI sheet; returns the next free row. */
  private aiListBlock(ws: import('exceljs').Worksheet, startRow: number, title: string, items: string[]): number {
    if (!items.length) {
      return startRow;
    }
    ws.getCell(`A${startRow}`).value = title;
    ws.getCell(`A${startRow}`).font = { bold: true, size: 11, color: { argb: 'FF0F172A' } };
    let row = startRow + 1;
    for (const item of items) {
      const cell = ws.getCell(`A${row}`);
      cell.value = `•  ${item}`;
      cell.alignment = { wrapText: true, vertical: 'top' };
      cell.font = { size: 10, color: { argb: 'FF0F172A' } };
      row++;
    }
    return row + 1;
  }

  private buildApisSheet(workbook: Workbook, report: IReportDetail): void {
    const ws = workbook.addWorksheet('APIs', { views: [{ state: 'frozen', ySplit: 1 }] });
    this.headerRow(
      ws.getRow(1),
      ['API', 'Method', 'Endpoint', 'Requests', 'Unchanged', 'Changed', 'Added', 'Removed', 'Failed', 'Breaking', 'Changed %', 'Risk'],
    );
    ws.columns.forEach((col, i) => (col.width = i === 0 ? 34 : i === 2 ? 40 : 12));

    let row = 2;
    for (const api of report.apis) {
      const changes = api.changedCount + api.addedCount + api.removedCount;
      const changePercent = api.requestCount > 0 ? changes / api.requestCount : 0;
      const risk = api.breakingCount > 0 ? 'High' : changes > 0 ? 'Medium' : 'Low';
      const r = ws.getRow(row);
      r.values = [
        api.apiName,
        api.method,
        api.endpoint,
        api.requestCount,
        api.unchangedCount,
        api.changedCount,
        api.addedCount,
        api.removedCount,
        api.failedCount,
        api.breakingCount,
        changePercent,
        risk,
      ];
      [4, 5, 6, 7, 8, 9, 10].forEach((c) => (r.getCell(c).numFmt = '#,##0'));
      r.getCell(11).numFmt = '0.00%';
      r.getCell(12).font = { bold: true, color: { argb: this.riskArgb(risk) } };
      r.eachCell((cell) => (cell.border = BORDER_ALL));
      row++;
    }
    ws.autoFilter = { from: { row: 1, column: 1 }, to: { row: 1, column: 12 } };
  }

  private buildBreakingSheet(workbook: Workbook, report: IReportDetail): void {
    const ws = workbook.addWorksheet('BreakingChanges', { views: [{ state: 'frozen', ySplit: 1 }] });
    this.headerRow(ws.getRow(1), ['API', 'Endpoint', 'Breaking Changes', 'Impact']);
    ws.columns = [{ width: 34 }, { width: 40 }, { width: 18 }, { width: 12 }];

    const breaking = report.apis
      .filter((api) => api.breakingCount > 0)
      .sort((a, b) => b.breakingCount - a.breakingCount);
    let row = 2;
    for (const api of breaking) {
      const r = ws.getRow(row);
      r.values = [`${api.method} ${api.apiName}`, api.endpoint, api.breakingCount, 'High'];
      r.getCell(3).numFmt = '#,##0';
      r.getCell(4).font = { bold: true, color: { argb: 'FFB91C1C' } };
      r.eachCell((cell) => (cell.border = BORDER_ALL));
      row++;
    }
    if (!breaking.length) {
      ws.getCell('A2').value = 'No breaking changes detected — safe to approve.';
      ws.getCell('A2').font = { color: { argb: HEADER_TEXT } };
    }
  }

  /** Loads the compare's field-level change summary (patterns per API), or null on failure. */
  private async fetchChangeSummary(compareId: number): Promise<ICompareChangeSummary | null> {
    try {
      return await firstValueFrom(this.compareApi.getChangeSummary(compareId));
    } catch {
      return null;
    }
  }

  /**
   * "Changes" sheet — one table per API (each with an API title header), listing that API's
   * field-level change patterns (mirrors the AI Insights tab).
   */
  private buildChangesSheet(workbook: Workbook, summary: ICompareChangeSummary): void {
    const ws = workbook.addWorksheet('Changes', { views: [{ showGridLines: false }] });
    ws.columns = [{ width: 16 }, { width: 40 }, { width: 12 }, { width: 10 }, { width: 9 }, { width: 60 }];

    const apisWithChanges = summary.apis.filter((api) => api.patterns.length > 0);
    if (!apisWithChanges.length) {
      ws.getCell('A1').value = 'No field-level changes.';
      ws.getCell('A1').font = { color: { argb: HEADER_TEXT } };
      return;
    }

    let row = 1;
    let first = true;
    for (const api of apisWithChanges) {
      if (!first) {
        row += 2; // blank spacer rows between API tables
      }
      first = false;

      // API title header (merged, two lines: name on top, endpoint + counts below).
      ws.mergeCells(row, 1, row, 6);
      const titleCell = ws.getCell(row, 1);
      const name = api.apiName ?? `API #${api.apiId}`;
      titleCell.value = {
        richText: [
          { text: `${api.method ?? ''}  `, font: { bold: true, size: 11, color: { argb: 'FF4F46E5' } } },
          { text: name, font: { bold: true, size: 11, color: { argb: 'FF0F172A' } } },
          {
            text: `\n${api.endpoint ?? ''}     ${api.patternCount} pattern(s) · ${api.requestCount} request(s)`,
            font: { size: 9, color: { argb: 'FF64748B' } },
          },
        ],
      };
      titleCell.alignment = { wrapText: true, vertical: 'middle' };
      titleCell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFEEF2FF' } };
      ws.getRow(row).height = 32;
      row++;

      // Column header for this API's table.
      this.headerRow(ws.getRow(row), ['Change Type', 'Path', 'Occurrences', 'Requests', 'Distinct', 'Samples (old → new)']);
      row++;

      // Pattern rows.
      for (const pattern of api.patterns) {
        const r = ws.getRow(row);
        r.values = [
          pattern.changeType,
          pattern.path,
          pattern.occurrences,
          pattern.requestCount,
          pattern.distinctTransitions,
          pattern.samples.map((s) => `${s.old ?? 'null'} → ${s.new ?? 'null'}`).join('  |  '),
        ];
        [3, 4, 5].forEach((c) => (r.getCell(c).numFmt = '#,##0'));
        r.getCell(6).alignment = { wrapText: true, vertical: 'top' };
        r.eachCell((cell) => (cell.border = BORDER_ALL));
        row++;
      }
    }
  }

  /** Loads every request's full detail (identity + result + raw field diff), or [] on failure. */
  private async fetchFullDetails(compareId: number): Promise<ICompareFullDetail[]> {
    try {
      return await firstValueFrom(this.compareApi.getFullDetails(compareId));
    } catch {
      return [];
    }
  }

  /**
   * "Full API" sheet — one row per field per request: request/endpoint + result + change type + values.
   * Changed requests expand into their field-level diffs; Added/Removed/Unchanged/Failed get one summary row.
   */
  private buildFullApiSheet(workbook: Workbook, details: ICompareFullDetail[]): void {
    const ws = workbook.addWorksheet('Full API', { views: [{ state: 'frozen', ySplit: 1 }] });
    this.headerRow(
      ws.getRow(1),
      ['Title', 'Request', 'Method', 'Endpoint', 'Result', 'Field', 'Change Type', 'Old Value', 'New Value'],
    );
    ws.columns = [
      { width: 22 }, { width: 46 }, { width: 8 }, { width: 40 }, { width: 12 }, { width: 40 }, { width: 14 }, { width: 40 }, { width: 40 },
    ];

    if (!details.length) {
      ws.getCell('A2').value = 'No request details.';
      ws.getCell('A2').font = { color: { argb: HEADER_TEXT } };
      return;
    }

    // Group per request: the first row of each request carries its identity (Request/Method/Endpoint/
    // Result); every field change is a row below it with those columns left blank — so it reads grouped.
    let row = 2;
    for (const d of details) {
      // Group header row: the request's identity only (fields go on the rows below).
      const idRow = ws.getRow(row);
      idRow.values = [d.title, this.requestInput(d), d.method, d.endpoint, d.result, '', '', '', ''];
      idRow.getCell(1).font = { bold: true, color: { argb: 'FF0F172A' } };
      // Keep the header row compact: don't wrap the (possibly long) JSON input — single line.
      idRow.getCell(2).alignment = { wrapText: false, vertical: 'middle' };
      for (let c = 1; c <= 9; c++) {
        idRow.getCell(c).fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFF1F5F9' } };
        idRow.getCell(c).border = { ...BORDER_ALL, top: { style: 'medium', color: { argb: 'FFCBD5E1' } } };
      }
      row++;

      // One row per field change, below the header (identity columns left blank).
      for (const fr of this.fullApiFieldRows(d)) {
        const r = ws.getRow(row);
        r.values = ['', '', '', '', '', fr.field, fr.changeType, fr.oldV, fr.newV];
        r.getCell(7).font = { bold: true, color: { argb: this.changeArgb(fr.changeType) } };
        r.getCell(8).alignment = { wrapText: true, vertical: 'top' };
        r.getCell(9).alignment = { wrapText: true, vertical: 'top' };
        for (let c = 1; c <= 9; c++) {
          r.getCell(c).border = BORDER_ALL;
        }
        row++;
      }
    }

    ws.autoFilter = { from: { row: 1, column: 1 }, to: { row: 1, column: 9 } };
  }

  /**
   * "Field Report" sheet — a focused change report: only diffs whose ROOT-level field matches the
   * comma-separated fields of interest (case-insensitive contains; empty = all). Rows are grouped by
   * (Change Type, Field): the pair is printed on the first row of each group only, so the sheet reads
   * as a grouped report. Columns: Change Type | Field | Old Value | New Value | Input Request.
   */
  private buildFieldReportSheet(workbook: Workbook, details: ICompareFullDetail[], fieldsCsv: string): void {
    const ws = workbook.addWorksheet('Field Report', { views: [{ state: 'frozen', ySplit: 1 }] });
    this.headerRow(ws.getRow(1), ['Change Type', 'Field', 'Path', 'Old Value', 'New Value', 'Input Request']);
    ws.columns = [{ width: 14 }, { width: 26 }, { width: 40 }, { width: 40 }, { width: 40 }, { width: 50 }];

    // Terms may be simple field names ("LEDStatus") or dotted paths ("VehicleInfo.FreezeFrames") to
    // disambiguate the same field under different parents. Leading "$." and array indices are ignored.
    const interests = fieldsCsv
      .split(',')
      .map((term) => term.trim().replace(/^\$\.?/, '').replace(/\[[^\]]*\]/g, ''))
      .filter((term) => term.length > 0);

    // Collect DISTINCT transitions: one row per (change type, field, normalized path, old, new).
    // The Input Request kept is the first request that produced the transition (a representative
    // for tracing); array indices are normalized to [*] so the same field across elements dedupes.
    const distinct = new Map<string, { changeType: string; field: string; path: string; oldV: string; newV: string; input: string }>();
    for (const d of details) {
      if (d.result !== 'Changed') {
        continue;
      }
      const input = this.requestInput(d) || d.title;
      for (const f of this.parseFieldDiffs(d.diffJson)) {
        const segments = this.pathSegments(f.path);
        const dotted = segments.join('.');
        let field: string;
        if (interests.length === 0) {
          field = segments[0] ?? f.path; // no filter → group by the root-level field
        } else {
          const matched = this.matchField(dotted, interests);
          if (matched === null) {
            continue;
          }
          field = matched;
        }
        const changeType = this.changeLabel(f.changeType);
        const path = f.path.replace(/\[\d+\]/g, '[*]');
        const oldV = this.cellValue(f.old);
        const newV = this.cellValue(f.new);
        const key = `${changeType}${field}${path}${oldV}${newV}`;
        if (!distinct.has(key)) {
          distinct.set(key, { changeType, field, path, oldV, newV, input });
        }
      }
    }
    const rows = [...distinct.values()];

    if (!rows.length) {
      ws.getCell('A2').value = interests.length
        ? `No changes found for: ${fieldsCsv}`
        : 'No field changes.';
      ws.getCell('A2').font = { color: { argb: HEADER_TEXT } };
      return;
    }

    // Group by (change type, field): stable sort, then print the pair only on each group's first row.
    rows.sort(
      (a, b) =>
        a.changeType.localeCompare(b.changeType) ||
        a.field.localeCompare(b.field) ||
        a.path.localeCompare(b.path) ||
        a.oldV.localeCompare(b.oldV),
    );

    let rowIndex = 2;
    let previousKey = '';
    for (const entry of rows) {
      const key = `${entry.changeType} ${entry.field}`;
      const isGroupStart = key !== previousKey;
      previousKey = key;

      const r = ws.getRow(rowIndex);
      r.values = [
        isGroupStart ? entry.changeType : '',
        isGroupStart ? entry.field : '',
        entry.path,
        entry.oldV,
        entry.newV,
        entry.input,
      ];
      if (isGroupStart) {
        r.getCell(1).font = { bold: true, color: { argb: this.changeArgb(entry.changeType) } };
        r.getCell(2).font = { bold: true, color: { argb: 'FF0F172A' } };
      }
      r.getCell(3).alignment = { wrapText: true, vertical: 'top' };
      r.getCell(4).alignment = { wrapText: true, vertical: 'top' };
      r.getCell(5).alignment = { wrapText: true, vertical: 'top' };
      r.getCell(6).alignment = { wrapText: false, vertical: 'top' };
      for (let c = 1; c <= 6; c++) {
        r.getCell(c).border = isGroupStart
          ? { ...BORDER_ALL, top: { style: 'medium', color: { argb: 'FFCBD5E1' } } }
          : BORDER_ALL;
      }
      rowIndex++;
    }

    ws.autoFilter = { from: { row: 1, column: 1 }, to: { row: 1, column: 6 } };
  }

  /** A diff path → its property segments with array indices stripped: "$.A[2].B" → ['A', 'B']. */
  private pathSegments(path: string): string[] {
    return path
      .replace(/^\$/, '')
      .replace(/\[\d+\]/g, '')
      .split('.')
      .filter((segment) => segment.length > 0);
  }

  /**
   * Matches the dotted path against the interest terms (case-insensitive substring; terms may be
   * dotted paths). Returns the path prefix up to the end of the segment where the FIRST matching term
   * ends — e.g. term "FreezeFrames" on "VehicleInfo.FreezeFrames.value" → "VehicleInfo.FreezeFrames",
   * so the same field under different parents groups (and filters) separately. Null = no match.
   */
  private matchField(dotted: string, interests: string[]): string | null {
    const lower = dotted.toLowerCase();
    for (const term of interests) {
      const pos = lower.indexOf(term.toLowerCase());
      if (pos < 0) {
        continue;
      }
      const nextDot = dotted.indexOf('.', pos + term.length);
      return dotted.slice(0, nextDot < 0 ? dotted.length : nextDot);
    }
    return null;
  }

  /** The request's sent input — query params and/or body as raw JSON (no labels; '' when none). */
  private requestInput(d: Pick<ICompareFullDetail, 'queryStringJson' | 'requestBody'>): string {
    const query = (d.queryStringJson ?? '').trim();
    const body = (d.requestBody ?? '').trim();
    const parts: string[] = [];
    if (query !== '' && query !== '{}') {
      parts.push(query);
    }
    if (body !== '' && body !== '{}') {
      parts.push(body);
    }
    return parts.join('\n');
  }

  /** The field-change rows for one request (Changed → per-field; others → one summary row). */
  private fullApiFieldRows(
    d: Pick<ICompareFullDetail, 'result' | 'diffJson'>,
  ): { field: string; changeType: string; oldV: string; newV: string }[] {
    switch (d.result) {
      case 'Changed': {
        const fields = this.parseFieldDiffs(d.diffJson);
        if (!fields.length) {
          return [{ field: '(changed)', changeType: 'Changed', oldV: '', newV: '' }];
        }
        return fields.map((f) => ({
          field: f.path,
          changeType: this.changeLabel(f.changeType),
          oldV: this.cellValue(f.old),
          newV: this.cellValue(f.new),
        }));
      }
      case 'Added':
        return [{ field: '(entire response)', changeType: 'Added', oldV: '', newV: '' }];
      case 'Removed':
        return [{ field: '(entire response)', changeType: 'Removed', oldV: '', newV: '' }];
      case 'Failed':
        return [{ field: '—', changeType: 'Failed', oldV: '', newV: '' }];
      default:
        return [{ field: '—', changeType: 'Unchanged', oldV: '', newV: '' }];
    }
  }

  /** Parse a request's stored field-diff JSON into entries; [] when null/invalid. */
  private parseFieldDiffs(diffJson: string | null): { path: string; changeType: string; old: unknown; new: unknown }[] {
    if (!diffJson) {
      return [];
    }
    try {
      const parsed = JSON.parse(diffJson);
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  /** A diff value → cell text (strings as-is, everything else JSON-stringified; null/undefined → ''). */
  private cellValue(value: unknown): string {
    if (value === null || value === undefined) {
      return '';
    }
    return typeof value === 'string' ? value : JSON.stringify(value);
  }

  /** Friendly label for a JsonDiffer change type. */
  private changeLabel(changeType: string): string {
    switch (changeType) {
      case 'ValueChanged':
        return 'Changed';
      case 'FieldAdded':
        return 'Added';
      case 'FieldRemoved':
        return 'Removed';
      case 'TypeChanged':
        return 'Type changed';
      case 'OrderChanged':
        return 'Order changed';
      default:
        return changeType;
    }
  }

  /** Colour (ARGB) for a change-type label in the Full API sheet. */
  private changeArgb(label: string): string {
    switch (label) {
      case 'Added':
        return 'FF6D28D9';
      case 'Removed':
        return 'FFB91C1C';
      case 'Failed':
        return 'FF991B1B';
      case 'Unchanged':
        return 'FF15803D';
      default:
        return 'FFB45309'; // Changed / Type changed
    }
  }

  /** Styles a row as a table header (fill + bold + borders). */
  private headerRow(row: import('exceljs').Row, headers: string[]): void {
    row.values = headers;
    row.eachCell((cell) => {
      cell.font = { bold: true, color: { argb: HEADER_TEXT } };
      cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: HEADER_FILL } };
      cell.border = BORDER_ALL;
      cell.alignment = { vertical: 'middle' };
    });
  }

  /** Renders the Change-Distribution donut (+ legend) to a PNG data URL via a canvas. */
  private renderDistributionPng(report: IReportDetail): string | null {
    const total = report.totalRequest;
    if (total <= 0) {
      return null;
    }
    const canvas = document.createElement('canvas');
    canvas.width = 420;
    canvas.height = 230;
    const ctx = canvas.getContext('2d');
    if (!ctx) {
      return null;
    }
    ctx.fillStyle = '#ffffff';
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    ctx.fillStyle = '#0f172a';
    ctx.font = 'bold 15px Arial';
    ctx.fillText('Change Distribution', 16, 26);

    const cx = 100;
    const cy = 130;
    const outer = 72;
    const inner = 42;
    let start = -Math.PI / 2;
    for (const slice of DIST) {
      const count = report[slice.key];
      if (count <= 0) {
        continue;
      }
      const end = start + (count / total) * Math.PI * 2;
      ctx.beginPath();
      ctx.moveTo(cx, cy);
      ctx.arc(cx, cy, outer, start, end);
      ctx.closePath();
      ctx.fillStyle = slice.color;
      ctx.fill();
      start = end;
    }
    ctx.beginPath();
    ctx.arc(cx, cy, inner, 0, Math.PI * 2);
    ctx.fillStyle = '#ffffff';
    ctx.fill();
    ctx.fillStyle = '#0f172a';
    ctx.font = 'bold 16px Arial';
    ctx.textAlign = 'center';
    ctx.fillText(total.toLocaleString('en-US'), cx, cy);
    ctx.font = '10px Arial';
    ctx.fillStyle = '#64748b';
    ctx.fillText('TOTAL', cx, cy + 15);
    ctx.textAlign = 'left';

    let ly = 70;
    for (const slice of DIST) {
      const count = report[slice.key];
      if (count <= 0) {
        continue;
      }
      ctx.fillStyle = slice.color;
      ctx.fillRect(210, ly - 9, 11, 11);
      ctx.fillStyle = '#0f172a';
      ctx.font = '12px Arial';
      const percent = Math.round((count / total) * 100);
      ctx.fillText(`${slice.label}  ${count.toLocaleString('en-US')} (${percent}%)`, 228, ly);
      ly += 22;
    }
    return canvas.toDataURL('image/png');
  }

  // ---------------- JSON ----------------

// ---------------- Test Results (Excel or ZIP with linked .txt files) ----------------

  /** Excel's hard cell limit is 32,767 chars — cap below it to leave room for the truncation marker. */
  private static readonly maxCellChars = 32_000;
  /** Folder (inside the ZIP, next to the workbook) that holds the TestCase_*.txt files. */
  private static readonly testCaseFolder = 'files';

  /**
   * "Test Results" export — the Runs-export layout for BOTH compared runs: one row per request with
   * the request input, run A's and run B's responses, the field changes and a Pass/Fail result
   * (Unchanged → Pass, anything else → Fail; the raw compare result is kept in its own column).
   * A second sheet lists every field change (one row each). In 'file' mode the request/response bodies
   * are written to TestCase_*.txt files and the cells link to them; everything ships as one ZIP.
   */
  private async buildTestResults(report: IReportDetail, output: TestResultsOutput): Promise<void> {
    // Sorted by Title (natural order, so "TC2" comes before "TC10"); API name / id only break ties.
    const rows = [...(await firstValueFrom(this.compareApi.getTestResults(report.compareJobId)))].sort(
      (a, b) =>
        a.title.localeCompare(b.title, undefined, { numeric: true, sensitivity: 'base' }) ||
        a.apiName.localeCompare(b.apiName) ||
        a.requestId - b.requestId,
    );
    const files = output === 'file' ? new Map<string, string>() : null;

    const workbook = new Workbook();
    workbook.creator = 'Regression Platform';
    workbook.created = new Date();

    const ws = workbook.addWorksheet('Test Results', { views: [{ state: 'frozen', ySplit: 1 }] });
    this.headerRow(ws.getRow(1), [
      'API', 'Method', 'Endpoint', 'Title', 'Request',
      `Response A (${report.runATitle})`, `Response B (${report.runBTitle})`,
      'Field Changes', 'Result', 'Compare Result', 'Status A', 'Status B', 'Time A (ms)', 'Time B (ms)', 'Error A', 'Error B',
    ]);
    ws.columns = [
      { width: 24 }, { width: 8 }, { width: 40 }, { width: 26 }, { width: 50 }, { width: 50 }, { width: 50 },
      { width: 50 }, { width: 8 }, { width: 14 }, { width: 9 }, { width: 9 }, { width: 11 }, { width: 11 }, { width: 30 }, { width: 30 },
    ];

    if (!rows.length) {
      ws.getCell('A2').value = 'No request details (compare not finished?).';
      ws.getCell('A2').font = { color: { argb: HEADER_TEXT } };
    }

    rows.forEach((row, index) => {
      const r = ws.getRow(index + 2);
      const passed = row.result === 'Unchanged';
      r.values = [
        row.apiName, row.method, row.endpoint, row.title, '', '', '',
        this.cellText(this.fieldChangeLines(row)),
        passed ? 'Pass' : 'Fail',
        row.result,
        row.statusCodeA ?? '', row.statusCodeB ?? '',
        row.durationMsA ?? '', row.durationMsB ?? '',
        row.errorMessageA ?? '', row.errorMessageB ?? '',
      ];
      const base = this.testCaseFileName(index + 1, row.title);
      this.bodyCell(r.getCell(5), this.requestInput(row), files, `${base}_request.txt`);
      this.bodyCell(r.getCell(6), row.responseAJson ?? '', files, `${base}_responseA.txt`);
      this.bodyCell(r.getCell(7), row.responseBJson ?? '', files, `${base}_responseB.txt`);
      r.getCell(8).alignment = { wrapText: false, vertical: 'top' };
      r.getCell(9).font = { bold: true, color: { argb: passed ? 'FF15803D' : 'FFB91C1C' } };
      r.getCell(10).font = { color: { argb: this.changeArgb(row.result) } };
    });
    ws.autoFilter = { from: { row: 1, column: 1 }, to: { row: 1, column: 16 } };

    this.buildFieldChangesSheet(workbook, rows);

    const name = `${displayReportId(report.id)}-test-results`;
    const buffer = await workbook.xlsx.writeBuffer();
    if (!files) {
      this.downloadBlob(
        new Blob([buffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' }),
        `${name}.xlsx`,
      );
      return;
    }

    // ZIP: the workbook at the root + files/TestCase_*.txt, so the relative links resolve once extracted.
    const zip = new JSZip();
    zip.file(`${name}.xlsx`, buffer);
    const folder = zip.folder(ReportExportService.testCaseFolder)!;
    for (const [fileName, content] of files) {
      folder.file(fileName, content);
    }
    const blob = await zip.generateAsync({ type: 'blob', compression: 'DEFLATE' });
    this.downloadBlob(blob, `${name}.zip`);
  }

  /** "Field Changes" sheet — one row per field change of every Changed request (full values, no cap per request). */
  private buildFieldChangesSheet(workbook: Workbook, rows: ICompareTestResultRow[]): void {
    const ws = workbook.addWorksheet('Field Changes', { views: [{ state: 'frozen', ySplit: 1 }] });
    this.headerRow(ws.getRow(1), ['Title', 'API', 'Field', 'Change Type', 'Old Value', 'New Value']);
    ws.columns = [{ width: 26 }, { width: 24 }, { width: 40 }, { width: 14 }, { width: 40 }, { width: 40 }];

    let rowIndex = 2;
    for (const row of rows) {
      if (row.result !== 'Changed') {
        continue;
      }
      for (const fr of this.fullApiFieldRows(row)) {
        const r = ws.getRow(rowIndex++);
        r.values = [row.title, row.apiName, fr.field, fr.changeType, this.cellText(fr.oldV), this.cellText(fr.newV)];
        r.getCell(4).font = { bold: true, color: { argb: this.changeArgb(fr.changeType) } };
        r.getCell(5).alignment = { wrapText: true, vertical: 'top' };
        r.getCell(6).alignment = { wrapText: true, vertical: 'top' };
      }
    }
    if (rowIndex === 2) {
      ws.getCell('A2').value = 'No field changes.';
      ws.getCell('A2').font = { color: { argb: HEADER_TEXT } };
    }
    ws.autoFilter = { from: { row: 1, column: 1 }, to: { row: 1, column: 6 } };
  }

  /** The request's field changes as one line each ("Changed  $.a.b: old → new"); a summary line otherwise. */
  private fieldChangeLines(row: ICompareTestResultRow): string {
    return this.fullApiFieldRows(row)
      .map((fr) => {
        const values = fr.oldV === '' && fr.newV === '' ? '' : `: ${this.shorten(fr.oldV)} → ${this.shorten(fr.newV)}`;
        return `${fr.changeType}  ${fr.field}${values}`;
      })
      .join('\n');
  }

  private shorten(text: string, max = 200): string {
    return text.length <= max ? text : `${text.slice(0, max)}…`;
  }

  /**
   * Writes a request/response body into a cell: in Excel mode the (capped) text itself; in file mode
   * the body is collected as a .txt file and the cell becomes a relative link to it (empty body → no file).
   */
  private bodyCell(cell: Cell, text: string, files: Map<string, string> | null, fileName: string): void {
    cell.alignment = { wrapText: false, vertical: 'top' };
    if (!files) {
      cell.value = this.cellText(text);
      return;
    }
    if (text === '') {
      cell.value = '';
      return;
    }
    files.set(fileName, text);
    const relative = `${ReportExportService.testCaseFolder}/${fileName}`;
    cell.value = { text: relative, hyperlink: relative, tooltip: 'Open the .txt file (extract the ZIP first)' };
    cell.font = { color: { argb: 'FF2563EB' }, underline: true };
  }

  /** "TestCase_0001_<title>" — a Windows-safe file-name stem, unique per row thanks to the index. */
  private testCaseFileName(index: number, title: string): string {
    const slug = title
      .replace(/[<>:"/\\|?*\u0000-\u001f]/g, '_')
      .replace(/\s+/g, '_')
      .replace(/_+/g, '_')
      .replace(/^[_.]+|[_.]+$/g, '')
      .slice(0, 60);
    return `TestCase_${String(index).padStart(4, '0')}${slug ? `_${slug}` : ''}`;
  }

  /** Caps a value at Excel's cell limit, appending an explicit marker so truncation is never silent. */
  private cellText(text: string): string {
    if (text.length <= ReportExportService.maxCellChars) {
      return text;
    }
    return `${text.slice(0, ReportExportService.maxCellChars)} …[TRUNCATED — full length ${text.length} chars; use the "Files" output to get the full body]`;
  }

  // ---------------- Live Data Result (professional template; always a ZIP with linked .json files) ----------------

  /** Live Data Result palette (simple, Excel-like): header grey, zebra, PASS green, FAILED/field rows light red. */
  private static readonly ldHeaderFill = 'FFD9D9D9';
  private static readonly ldZebraFill = 'FFF5F5F5';
  private static readonly ldPassFill = 'FFCCFFCC';
  private static readonly ldPassText = 'FF00B050';
  private static readonly ldFailFill = 'FFFAE2DE';
  private static readonly ldFailText = 'FFFF0000';
  private static readonly ldColumnCount = 12;
  private static readonly ldFieldHeaderFill = 'FFDDEBF7';

  /**
   * "Live Data Result" export — the review template:
   * Index | Test Case Id | Test Name | request | Language | App | Field | Status | <date> Expected Result | <date> Response | Expected Time (ms) | Response Time (ms).
   * One "Summary" row per request (Test Case Id = Title, Test Name = Description, Status PASS/FAILED, the
   * request + both runs' responses linked as .json files under requests/ · expected_response/ · response/).
   * A FAILED request is followed by one row per changed field (Field = path, Status = Different,
   * Expected Result / Response = old / new value). Everything ships as one ZIP named after the report.
   */
  private async buildLiveDataResult(report: IReportDetail, context: IReportPdfContext, splitRequest: boolean): Promise<void> {
    // Same ordering as the other exports: Title (natural), API / id only break ties — Index follows it.
    const rows = [...(await firstValueFrom(this.compareApi.getTestResults(report.compareJobId)))].sort(
      (a, b) =>
        a.title.localeCompare(b.title, undefined, { numeric: true, sensitivity: 'base' }) ||
        a.apiName.localeCompare(b.apiName) ||
        a.requestId - b.requestId,
    );
    const files = new Map<string, string>();
    const datePrefix = this.dateStamp(report.generatedDate);
    const templateColumns = ReportExportService.ldColumnCount;

    // Optional: one extra column per request field (import-Excel layout), after the template columns.
    const requestFields = splitRequest ? rows.map((row) => this.requestFieldMap(row)) : [];
    const fieldNames = [...new Set(requestFields.flatMap((fields) => [...fields.keys()]))];
    const columns = templateColumns + fieldNames.length;

    const workbook = new Workbook();
    workbook.creator = 'Regression Platform';
    workbook.created = new Date();

    const ws = workbook.addWorksheet('Live Data Result', { views: [{ state: 'frozen', ySplit: 1 }] });
    const header = ws.getRow(1);
    header.values = [
      'Index', 'Test Case Id', 'Test Name', 'request', 'Language', 'App', 'Field', 'Status',
      `${datePrefix} Expected Result`, `${datePrefix} Response`, 'Expected Time (ms)', 'Response Time (ms)',
      ...fieldNames,
    ];
    header.eachCell((cell, column) => {
      cell.font = { bold: true, color: { argb: 'FF000000' } };
      // Request-field columns get a light blue header so they read apart from the template columns.
      const fill = column > templateColumns ? ReportExportService.ldFieldHeaderFill : ReportExportService.ldHeaderFill;
      cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: fill } };
      cell.border = BORDER_ALL;
      cell.alignment = { vertical: 'middle' };
    });
    ws.columns = [
      { width: 8 }, { width: 28 }, { width: 32 }, { width: 26 }, { width: 10 }, { width: 12 },
      { width: 34 }, { width: 10 }, { width: 36 }, { width: 36 }, { width: 14 }, { width: 14 },
      ...fieldNames.map((name) => ({ width: Math.min(40, Math.max(14, name.length + 2)) })),
    ];

    if (!rows.length) {
      ws.getCell('A2').value = 'No request details (compare not finished?).';
      ws.getCell('A2').font = { color: { argb: HEADER_TEXT } };
    }

    let rowIndex = 2;
    rows.forEach((row, i) => {
      const index = i + 1;
      const passed = row.result === 'Unchanged';

      // Summary row — the request itself (zebra on odd indexes, as in the reference file).
      const summary = ws.getRow(rowIndex++);
      summary.getCell(1).value = index;
      summary.getCell(2).value = row.title;
      summary.getCell(3).value = row.description ?? '';
      this.fileCell(summary.getCell(4), this.requestInput(row), files, `requests/row_${index}.json`);
      summary.getCell(5).value = '';
      summary.getCell(6).value = context.projectName ?? '';
      summary.getCell(7).value = 'Summary';
      summary.getCell(8).value = passed ? 'PASS' : 'FAILED';
      this.fileCell(summary.getCell(9), row.responseAJson ?? '', files, `expected_response/row_${index}.json`);
      this.fileCell(summary.getCell(10), row.responseBJson ?? '', files, `response/row_${index}.json`);
      summary.getCell(11).value = row.durationMsA ?? '';
      summary.getCell(12).value = row.durationMsB ?? '';
      if (splitRequest) {
        const fields = requestFields[i];
        fieldNames.forEach((name, offset) => {
          const value = fields.get(name);
          const cell = summary.getCell(templateColumns + 1 + offset);
          cell.value = value === undefined ? '' : typeof value === 'string' ? this.cellText(value) : value;
          cell.alignment = { wrapText: false, vertical: 'top' };
        });
      }
      for (let c = 1; c <= columns; c++) {
        const cell = summary.getCell(c);
        cell.border = BORDER_ALL;
        if (index % 2 === 1 && c !== 8) {
          cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: ReportExportService.ldZebraFill } };
        }
      }
      const status = summary.getCell(8);
      status.alignment = { horizontal: 'center', vertical: 'middle' };
      status.fill = {
        type: 'pattern',
        pattern: 'solid',
        fgColor: { argb: passed ? ReportExportService.ldPassFill : ReportExportService.ldFailFill },
      };
      status.font = { bold: true, color: { argb: passed ? ReportExportService.ldPassText : ReportExportService.ldFailText } };

      // Detail rows — one per changed field, light-red like the reference ("Different", old → new).
      if (passed) {
        return;
      }
      for (const fr of this.liveDataFieldRows(row)) {
        const detail = ws.getRow(rowIndex++);
        detail.getCell(7).value = fr.field;
        detail.getCell(8).value = fr.status;
        detail.getCell(9).value = this.cellText(fr.oldV);
        detail.getCell(10).value = this.cellText(fr.newV);
        detail.getCell(9).alignment = { wrapText: false, vertical: 'top' };
        detail.getCell(10).alignment = { wrapText: false, vertical: 'top' };
        detail.getCell(8).alignment = { horizontal: 'center', vertical: 'middle' };
        for (let c = 1; c <= columns; c++) {
          const cell = detail.getCell(c);
          cell.border = BORDER_ALL;
          cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: ReportExportService.ldFailFill } };
        }
      }
    });
    ws.autoFilter = { from: { row: 1, column: 1 }, to: { row: 1, column: columns } };

    const name = displayReportId(report.id);
    const buffer = await workbook.xlsx.writeBuffer();
    const zip = new JSZip();
    zip.file(`${name}.xlsx`, buffer);
    for (const [path, content] of files) {
      zip.file(path, content);
    }
    const blob = await zip.generateAsync({ type: 'blob', compression: 'DEFLATE' });
    this.downloadBlob(blob, `${name}.zip`);
  }

  /**
   * Writes a body to its own .json file (at the given relative path) and makes the cell a link to it.
   * Empty content → an empty cell and no file. Used by the Live Data Result template (always file-linked).
   */
  private fileCell(cell: Cell, content: string, files: Map<string, string>, path: string): void {
    cell.alignment = { wrapText: false, vertical: 'top' };
    if (content === '') {
      cell.value = '';
      return;
    }
    files.set(path, content);
    cell.value = { text: path, hyperlink: path, tooltip: 'Open the file (extract the ZIP first)' };
    cell.font = { color: { argb: 'FF2563EB' }, underline: true };
  }

  /**
   * The detail rows under a FAILED request: one per changed field with the reference file's wording
   * (Different / Missing / Added / Type changed); Added/Removed/Failed requests get one explanatory row.
   */
  private liveDataFieldRows(row: ICompareTestResultRow): { field: string; status: string; oldV: string; newV: string }[] {
    switch (row.result) {
      case 'Changed': {
        const fields = this.parseFieldDiffs(row.diffJson);
        if (!fields.length) {
          return [{ field: '(response)', status: 'Different', oldV: '', newV: '' }];
        }
        return fields.map((f) => ({
          field: f.path,
          status: this.liveDataStatus(f.changeType),
          oldV: this.cellValue(f.old),
          newV: this.cellValue(f.new),
        }));
      }
      case 'Added':
        return [{ field: '(entire response)', status: 'Added', oldV: '', newV: '(present)' }];
      case 'Removed':
        return [{ field: '(entire response)', status: 'Missing', oldV: '(present)', newV: '' }];
      case 'Failed':
        return [{ field: '(request)', status: 'Error', oldV: row.errorMessageA ?? '', newV: row.errorMessageB ?? '' }];
      default:
        return [];
    }
  }

  /**
   * A request's input flattened like the request import Excel: nested objects → dotted column names
   * ("VehicleInfo.Vin"), arrays / empty objects → JSON text, null → `null`, empty string → `""`
   * (the import's literals), numbers / booleans kept typed. Query string first, then body.
   * A non-object body (e.g. a top-level array) goes into one "(body)" column as JSON text.
   */
  private requestFieldMap(row: Pick<ICompareTestResultRow, 'queryStringJson' | 'requestBody'>): Map<string, string | number | boolean> {
    const fields = new Map<string, string | number | boolean>();
    const walk = (value: unknown, path: string): void => {
      if (value === null) {
        fields.set(path, 'null');
      } else if (Array.isArray(value)) {
        fields.set(path, JSON.stringify(value));
      } else if (typeof value === 'object') {
        const entries = Object.entries(value as Record<string, unknown>);
        if (!entries.length) {
          fields.set(path, '{}');
          return;
        }
        for (const [key, child] of entries) {
          walk(child, path ? `${path}.${key}` : key);
        }
      } else if (typeof value === 'string') {
        fields.set(path, value === '' ? '""' : value);
      } else if (typeof value === 'number' || typeof value === 'boolean') {
        fields.set(path, value);
      }
    };
    for (const json of [row.queryStringJson, row.requestBody]) {
      const text = (json ?? '').trim();
      if (!text || text === '{}') {
        continue;
      }
      let parsed: unknown;
      try {
        parsed = JSON.parse(text);
      } catch {
        fields.set('(body)', text);
        continue;
      }
      if (parsed !== null && typeof parsed === 'object' && !Array.isArray(parsed)) {
        walk(parsed, '');
      } else {
        fields.set('(body)', JSON.stringify(parsed));
      }
    }
    return fields;
  }

  /** JsonDiffer change type → the Live Data Result status word. */
  private liveDataStatus(changeType: string): string {
    switch (changeType) {
      case 'FieldAdded':
        return 'Added';
      case 'FieldRemoved':
        return 'Missing';
      case 'TypeChanged':
        return 'Type changed';
      case 'OrderChanged':
        return 'Order changed';
      default:
        return 'Different';
    }
  }

  /** A local YYYY-MM-DD stamp from an ISO date (today when the date is missing/invalid). */
  private dateStamp(iso: string): string {
    const date = new Date(iso);
    const value = Number.isNaN(date.getTime()) ? new Date() : date;
    const month = String(value.getMonth() + 1).padStart(2, '0');
    const day = String(value.getDate()).padStart(2, '0');
    return `${value.getFullYear()}-${month}-${day}`;
  }

  private async buildJson(
    report: IReportDetail,
    context: IReportPdfContext,
    options: IReportExportOptions,
    ai: IReportAiAnalysis | null,
  ): Promise<void> {
    const total = report.totalRequest;
    const changeRate = (api: IReportApiStat): number =>
      api.requestCount > 0 ? (api.changedCount + api.addedCount + api.removedCount) / api.requestCount : 0;
    const round2 = (value: number): number => Math.round(value * 10000) / 10000;

    const payload: Record<string, unknown> = {
      reportId: displayReportId(report.id),
      compareId: displayCompareId(report.compareJobId),
      project: context.projectName,
      name: report.name,
      status: report.status,
      generatedAt: report.generatedDate,
      runA: report.runATitle,
      runB: report.runBTitle,
      regressionScore: report.regressionScore,
      riskLevel: report.riskLevel,
      deploymentRecommendation: this.recommendation(report.riskLevel),
      totals: {
        totalRequests: report.totalRequest,
        unchanged: report.unchangedCount,
        changed: report.changedCount,
        added: report.addedCount,
        removed: report.removedCount,
        failed: report.failedCount,
        totalApis: report.totalApis,
        breakingApis: report.breakingApiCount,
      },
      topChangedApis: [...report.apis]
        .filter((api) => api.changedCount > 0)
        .sort((a, b) => b.changedCount - a.changedCount)
        .slice(0, 5)
        .map((api) => ({
          api: api.apiName,
          method: api.method,
          endpoint: api.endpoint,
          changed: api.changedCount,
          added: api.addedCount,
          removed: api.removedCount,
          percentOfTotal: round2(total > 0 ? api.changedCount / total : 0),
        })),
      breakingApis: report.apis
        .filter((api) => api.breakingCount > 0)
        .sort((a, b) => b.breakingCount - a.breakingCount)
        .map((api) => ({ api: api.apiName, method: api.method, endpoint: api.endpoint, breakingCount: api.breakingCount })),
    };

    if (ai) {
      payload['aiSummary'] = {
        provider: ai.provider,
        generatedAt: ai.generatedAt,
        summary: ai.summary,
        findings: ai.findings,
        recommendation: ai.recommendation,
        riskLevel: ai.riskLevel,
        riskScore: ai.riskScore,
        riskFactors: ai.riskFactors,
        recommendations: ai.recommendations,
      };
    }

    if (options.includeDetailed) {
      payload['apis'] = report.apis.map((api: IReportApiStat) => ({
        ...api,
        changePercent: round2(changeRate(api)),
        risk: api.breakingCount > 0 ? 'High' : api.changedCount + api.addedCount + api.removedCount > 0 ? 'Medium' : 'Low',
      }));

      // Field-level change patterns per API (mirrors the compare "AI Insights" tab).
      const changeSummary = await this.fetchChangeSummary(report.compareJobId);
      if (changeSummary) {
        payload['changes'] = changeSummary.apis.map((api) => ({
          apiId: api.apiId,
          api: api.apiName,
          method: api.method,
          endpoint: api.endpoint,
          requestCount: api.requestCount,
          patternCount: api.patternCount,
          patterns: api.patterns.map((pattern) => ({
            changeType: pattern.changeType,
            path: pattern.path,
            occurrences: pattern.occurrences,
            requestCount: pattern.requestCount,
            distinctTransitions: pattern.distinctTransitions,
            samples: pattern.samples.map((sample) => ({ old: sample.old, new: sample.new })),
          })),
        }));
      }
    }

    const blob = new Blob([JSON.stringify(payload, null, 2)], { type: 'application/json' });
    this.downloadBlob(blob, `${displayReportId(report.id)}.json`);
  }

  // ---------------- shared ----------------

  private recommendation(risk: string): string {
    switch (risk) {
      case 'Low':
        return 'Approved';
      case 'Medium':
        return 'Conditional Approval';
      case 'High':
        return 'Needs Review';
      default:
        return 'Not Recommended';
    }
  }

  private riskArgb(risk: string): string {
    switch (risk) {
      case 'High':
        return 'FFB91C1C';
      case 'Medium':
        return 'FFB45309';
      default:
        return 'FF15803D';
    }
  }

  private ratio(count: number, total: number): number {
    return total > 0 ? count / total : 0;
  }

  private downloadBlob(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    this.downloadUrl(url, fileName);
    setTimeout(() => URL.revokeObjectURL(url), 4000);
  }

  private downloadUrl(url: string, fileName: string): void {
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
  }

  private formatDate(iso: string): string {
    const date = new Date(iso);
    return Number.isNaN(date.getTime()) ? iso : date.toLocaleString('en-GB');
  }
}
