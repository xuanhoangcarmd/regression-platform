import { Injectable, inject } from '@angular/core';
import { Workbook } from 'exceljs';
import { firstValueFrom } from 'rxjs';

import { RunApiService } from '../apis/run-api.service';
import { IRunResponseExportRow } from '../models/run.model';
import { displayRunId } from './run.service';

/**
 * Excel export of a run's executed requests: API, request input, full response and timing — one row
 * per request. Excel caps a cell at 32,767 characters, so oversized request/response bodies are
 * truncated at a safe limit with an explicit marker (the full body stays viewable on the Results tab).
 */
@Injectable({ providedIn: 'root' })
export class RunExportService {
  private readonly api = inject(RunApiService);

  /** Excel's hard cell limit is 32,767 chars — cap below it to leave room for the truncation marker. */
  private static readonly maxCellChars = 32_000;

  /** Fetches the run's rows and downloads them as a styled .xlsx. Resolves when the download starts. */
  async export(runId: number): Promise<void> {
    const rows = await firstValueFrom(this.api.getExport(runId));

    const workbook = new Workbook();
    workbook.creator = 'Regression Platform';
    workbook.created = new Date();

    const ws = workbook.addWorksheet('Responses', { views: [{ state: 'frozen', ySplit: 1 }] });
    ws.columns = [
      { width: 24 }, { width: 8 }, { width: 40 }, { width: 26 }, { width: 50 }, { width: 60 }, { width: 8 }, { width: 10 }, { width: 30 },
    ];

    const header = ws.getRow(1);
    header.values = ['API', 'Method', 'Endpoint', 'Title', 'Request', 'Response', 'Status', 'Time (ms)', 'Error'];
    header.font = { bold: true, color: { argb: 'FF475569' } };
    header.eachCell((cell) => {
      cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFF1F5F9' } };
    });

    let rowIndex = 2;
    for (const row of rows) {
      const r = ws.getRow(rowIndex);
      r.values = [
        row.apiName,
        row.method,
        row.endpoint,
        row.title,
        this.cellText(this.requestInput(row)),
        this.cellText(row.responseJson ?? ''),
        row.statusCode ?? '',
        row.durationMs ?? '',
        row.errorMessage ?? '',
      ];
      // Long JSON stays on one line (no wrap) so rows keep a sane height; users expand as needed.
      r.getCell(5).alignment = { wrapText: false, vertical: 'top' };
      r.getCell(6).alignment = { wrapText: false, vertical: 'top' };
      rowIndex++;
    }

    ws.autoFilter = { from: { row: 1, column: 1 }, to: { row: 1, column: 9 } };

    const buffer = await workbook.xlsx.writeBuffer();
    const blob = new Blob([buffer], {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = `${displayRunId(runId)}-responses.xlsx`;
    anchor.click();
    URL.revokeObjectURL(url);
  }

  /** The request's sent input — query params and/or body as raw JSON ('' when none). */
  private requestInput(row: IRunResponseExportRow): string {
    const query = (row.requestQueryJson ?? '').trim();
    const body = (row.requestBody ?? '').trim();
    const parts: string[] = [];
    if (query !== '' && query !== '{}') {
      parts.push(query);
    }
    if (body !== '' && body !== '{}') {
      parts.push(body);
    }
    return parts.join('\n');
  }

  /** Caps a value at Excel's cell limit, appending an explicit marker so truncation is never silent. */
  private cellText(text: string): string {
    if (text.length <= RunExportService.maxCellChars) {
      return text;
    }
    return `${text.slice(0, RunExportService.maxCellChars)} …[TRUNCATED — full length ${text.length} chars, view in Results tab]`;
  }
}
