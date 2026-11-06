import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

import { ModalComponent } from '@shared/components';

import { IReportDetail } from '../../../models/report.model';
import { IReportExportOptions, ReportExportFormat, TestResultsOutput } from '../../../services/report-export.service';
import { displayReportId } from '../../../services/report.service';

/** The chosen export format + options, emitted to the parent (which runs the export + shows progress). */
export interface IReportExportRequest {
  format: ReportExportFormat;
  options: IReportExportOptions;
}

interface FormatOption {
  value: ReportExportFormat;
  title: string;
  desc: string;
  icon: string;
  accent: string;
}

/** Export a report to PDF / Excel / CSV / JSON with a few content options. */
@Component({
  selector: 'app-report-export-modal',
  standalone: true,
  imports: [MatIconModule, ModalComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './report-export-modal.component.html',
})
export class ReportExportModalComponent {
  readonly report = input.required<IReportDetail>();
  readonly projectName = input<string>('');
  readonly closed = output<void>();
  /** Emits the chosen format + options; the parent runs the export and shows progress on the page. */
  readonly submitted = output<IReportExportRequest>();

  protected readonly displayReportId = displayReportId;

  protected readonly selectedFormat = signal<ReportExportFormat>('pdf');
  protected readonly includeCharts = signal(true);
  protected readonly includeDetailed = signal(true);
  protected readonly includeAiSummary = signal(true);
  /** Off by default; only meaningful for Excel (adds the per-field "Full API" sheet). */
  protected readonly includeFullApi = signal(false);
  /** Off by default; only meaningful for Excel (adds the focused "Field Report" sheet). */
  protected readonly includeFieldReport = signal(false);
  /** Root fields of interest for the Field Report (comma-separated; contains-match, case-insensitive). */
  protected readonly fieldReportFields = signal('LEDStatus, FF, DTCs');
  /** Test Results only: bodies inside the cells, or as linked TestCase_*.txt files in a ZIP. */
  protected readonly testResultsOutput = signal<TestResultsOutput>('excel');
  /** Live Data Result only: also split each request's fields into their own columns. */
  protected readonly liveDataSplitRequest = signal(false);

  /** The Test Results format has its own output choice instead of the content options. */
  protected readonly isTestResults = computed(() => this.selectedFormat() === 'testresults');
  /** The Live Data Result template is always a ZIP with linked .json files — no content options. */
  protected readonly isLiveData = computed(() => this.selectedFormat() === 'livedata');

  protected readonly outputs: { value: TestResultsOutput; title: string; desc: string }[] = [
    {
      value: 'excel',
      title: 'Excel',
      desc: 'Request / responses inside the cells (bodies longer than 32,000 characters are truncated with a marker).',
    },
    {
      value: 'file',
      title: 'Files (ZIP)',
      desc: 'Request / responses saved as TestCase_….txt files; the Excel cells link to them. Extract the ZIP, then open the workbook.',
    },
  ];

  /** Charts only apply to the PDF format. */
  protected readonly chartsDisabled = computed(() => this.selectedFormat() !== 'pdf');
  /** The "Full API" sheet is Excel-only. */
  protected readonly fullApiDisabled = computed(() => this.selectedFormat() !== 'excel');
  /** The "Field Report" sheet is Excel-only. */
  protected readonly fieldReportDisabled = computed(() => this.selectedFormat() !== 'excel');

  protected readonly formats: FormatOption[] = [
    { value: 'pdf', title: 'PDF Document', desc: 'Full report with charts and summary', icon: 'picture_as_pdf', accent: 'bg-red-50 text-red-600' },
    { value: 'excel', title: 'Excel Spreadsheet', desc: 'Summary + per-API sheets (.xlsx)', icon: 'grid_on', accent: 'bg-green-50 text-green-600' },
    { value: 'testresults', title: 'Test Results (Excel)', desc: 'One row per request: request, both runs\' responses, field changes, Pass/Fail', icon: 'fact_check', accent: 'bg-emerald-50 text-emerald-700' },
    { value: 'livedata', title: 'Live Data Result (ZIP)', desc: 'Flat template (Index, Test Case Id, Field, Status…) with request + both responses as linked .json files', icon: 'biotech', accent: 'bg-sky-50 text-sky-700' },
    { value: 'json', title: 'JSON File', desc: 'Structured report data as JSON', icon: 'data_object', accent: 'bg-violet-50 text-violet-600' },
  ];

  protected save(): void {
    // Hand the request to the parent (which runs the export + shows progress on the page) and close.
    this.submitted.emit({
      format: this.selectedFormat(),
      options: {
        includeCharts: this.includeCharts(),
        includeDetailed: this.includeDetailed(),
        includeAiSummary: this.includeAiSummary(),
        includeFullApi: this.includeFullApi() && !this.fullApiDisabled(),
        includeFieldReport: this.includeFieldReport() && !this.fieldReportDisabled(),
        fieldReportFields: this.fieldReportFields().trim(),
        testResultsOutput: this.testResultsOutput(),
        liveDataSplitRequest: this.isLiveData() && this.liveDataSplitRequest(),
      },
    });
    this.closed.emit();
  }
}
