import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { forkJoin } from 'rxjs';

import { IPagedResult } from '@core/models';
import { AppIconsConstants } from '@core/constants';
import {
  ButtonComponent,
  CellTemplateDirective,
  DataTableComponent,
  HeaderTemplateDirective,
  IDataTableColumn,
  ISelectOption,
  InputComponent,
  PaginatorComponent,
  SelectComponent,
  StatusBadgeComponent,
} from '@shared/components';

import { PayloadGeneratorModalComponent } from '@features/payload-generator/components/payload-generator-modal/payload-generator-modal.component';

import { DataLibraryApiService } from '../../apis/data-library-api.service';
import {
  DtcMatchMode,
  IVehicleProfileDetail,
  IVehicleProfileListItem,
  IVehicleProfileSearch,
  IYmmeOptions,
  VehicleProfileSortField,
} from '../../models/data-library.model';

/** A decompressed base64 blob (payload or raw buffer) shown in the inline viewer. */
interface IBase64View {
  label: string;
  base64: string;
  byteSize: number | null;
}

/** Data Library: search vehicle payloads/raw buffers by YMME / DTC / MIL, then inspect a profile. */
@Component({
  selector: 'app-data-library-page',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatIconModule,
    ButtonComponent,
    InputComponent,
    SelectComponent,
    DataTableComponent,
    CellTemplateDirective,
    HeaderTemplateDirective,
    StatusBadgeComponent,
    PaginatorComponent,
    PayloadGeneratorModalComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './data-library-page.component.html',
})
export class DataLibraryPageComponent {
  private readonly api = inject(DataLibraryApiService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formBuilder = inject(FormBuilder);

  private static readonly emptyPage: IPagedResult<IVehicleProfileListItem> = {
    items: [],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0,
  };

  protected readonly ymme = signal<IYmmeOptions>({ years: [], makes: [], models: [], engines: [] });

  protected readonly result = signal<IPagedResult<IVehicleProfileListItem>>(DataLibraryPageComponent.emptyPage);
  protected readonly pageNumber = signal(1);
  protected readonly pageSize = signal(10);
  protected readonly loading = signal(false);

  protected readonly detail = signal<IVehicleProfileDetail | null>(null);
  protected readonly selectedId = signal<number | null>(null);
  protected readonly base64View = signal<IBase64View | null>(null);

  /** True while a profile detail is being fetched (click VIN / view). */
  protected readonly detailLoading = signal(false);
  /** True while a single payload/raw-buffer base64 is being fetched for the inline viewer. */
  protected readonly base64Loading = signal(false);

  /** Payload Generator modal open state (opened from the header button). */
  protected readonly showGeneratorModal = signal(false);

  protected readonly searchIcon = AppIconsConstants.search;
  protected readonly generatorIcon = AppIconsConstants.payloadGenerator;
  protected readonly viewIcon = AppIconsConstants.view;
  protected readonly copyIcon = AppIconsConstants.copy;
  protected readonly checkIcon = AppIconsConstants.success;
  protected readonly closeIcon = AppIconsConstants.close;

  /** Brief "Copied" feedback after clicking Copy in the base64 viewer. */
  protected readonly copied = signal(false);
  /** True while fetching every raw buffer's content to assemble the combined JSON. */
  protected readonly buffersJsonLoading = signal(false);
  protected readonly expandMoreIcon = AppIconsConstants.expandMore;
  protected readonly expandLessIcon = AppIconsConstants.expandLess;

  /** DTC filters are collapsed by default (there are many) — expand to use them. */
  protected readonly dtcExpanded = signal(false);

  /** The secondary DTC attribute filters (Source/System/Status/Definition) are hidden until opted in. */
  protected readonly dtcMoreExpanded = signal(false);

  /** Year-range filter is hidden until the user opts in (advanced filter). */
  protected readonly yearRangeExpanded = signal(false);

  /** True when any DTC filter currently has a value (shown as an "active" hint while collapsed). */
  protected hasDtcFilter(): boolean {
    const v = this.form.getRawValue();
    return !!(
      v.primaryDtc || v.dtcCode || v.dtcSource || v.dtcSystem || v.dtcStatusType || v.dtcDefinition || v.dtcFormat || v.dtcPrefix
    );
  }

  protected readonly form = this.formBuilder.nonNullable.group({
    vin: '',
    year: '',
    yearFrom: '',
    yearTo: '',
    make: '',
    model: '',
    engine: '',
    mil: '',
    led: '',
    hasRawBuffer: '',
    primaryDtc: '',
    primaryDtcMode: 'contains',
    dtcCode: '',
    dtcCodeMode: 'contains',
    dtcSource: '',
    dtcSystem: '',
    dtcStatusType: '',
    dtcDefinition: '',
    dtcFormat: '',
    dtcPrefix: '',
  });

  /** DTC code-prefix categories (P/B/C/U system letter, starts-with-number, or none of P/B/C/U). */
  protected readonly dtcPrefixOptions: ISelectOption[] = [
    { value: '', label: 'Any prefix' },
    { value: 'P', label: 'P — Powertrain' },
    { value: 'B', label: 'B — Body' },
    { value: 'C', label: 'C — Chassis' },
    { value: 'U', label: 'U — Network' },
    { value: 'not-pbcu', label: 'Not P/B/C/U' },
    { value: 'number', label: 'Starts with a number' },
    { value: 'not-pbcu-num', label: 'Not P/B/C/U or number' },
    { value: 'not-junk', label: "Exclude 'No code' / 'Available'" },
  ];

  /** Match-mode options for the Primary/OEM DTC keyword inputs. */
  protected readonly dtcMatchModeOptions: ISelectOption[] = [
    { value: 'contains', label: 'Contains' },
    { value: 'equals', label: 'Equals' },
    { value: 'startsWith', label: 'Starts with' },
    { value: 'endsWith', label: 'Ends with' },
  ];

  /** DTC code-format presets — matched by shape only (value = backend key). */
  protected readonly dtcFormatOptions: ISelectOption[] = [
    { value: '', label: 'Any format' },
    // No-dash shapes (by length)
    { value: 'xx', label: 'xx' },
    { value: 'xxxx', label: 'xxxx' },
    { value: 'xxxxx', label: 'xxxxx' },
    { value: 'xxxxxx', label: 'xxxxxx' },
    { value: 'xxxxxxx', label: 'xxxxxxx' },
    { value: 'xxxxxxxx', label: 'xxxxxxxx' },
    // Dashed shapes
    { value: 'x-x', label: 'x-x' },
    { value: 'x-xx', label: 'x-xx' },
    { value: 'x-xxx', label: 'x-xxx' },
    { value: 'xx-x', label: 'xx-x' },
    { value: 'xx-xx', label: 'xx-xx' },
    { value: 'xx-xxx', label: 'xx-xxx' },
    { value: 'xxx-x', label: 'xxx-x' },
    { value: 'xxx-xx', label: 'xxx-xx' },
    { value: 'xxxx-xxx', label: 'xxxx-xxx' },
    { value: 'xxxxx-xx', label: 'xxxxx-xx' },
    { value: 'xxxxx-xxx', label: 'xxxxx-xxx' },
  ];

  // ─── YMME table sort (server-side) ───
  /** Current sort field for the YMME column, or null (default = newest first). */
  protected readonly sortBy = signal<VehicleProfileSortField | null>(null);
  protected readonly sortDir = signal<'asc' | 'desc'>('asc');
  /** Open state of the YMME header sort popover. */
  protected readonly sortMenuOpen = signal(false);
  /** Viewport coords for the popover (fixed-positioned so the table's overflow can't clip it). */
  protected readonly sortMenuPos = signal<{ x: number; y: number }>({ x: 0, y: 0 });

  /** Fields offered in the YMME sort popover (matches the column: Year / Make / Model / Engine). */
  protected readonly sortFields: { field: VehicleProfileSortField; label: string }[] = [
    { field: 'year', label: 'Year' },
    { field: 'make', label: 'Make' },
    { field: 'model', label: 'Model' },
    { field: 'engine', label: 'Engine Type' },
  ];

  protected toggleSortMenu(event: MouseEvent): void {
    const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
    this.sortMenuPos.set({ x: rect.left, y: rect.bottom + 4 });
    this.sortMenuOpen.update((open) => !open);
  }

  /** Pick a sort field: same field flips direction, a new field starts ascending. Re-searches. */
  protected applySort(field: VehicleProfileSortField): void {
    if (this.sortBy() === field) {
      this.sortDir.update((dir) => (dir === 'asc' ? 'desc' : 'asc'));
    } else {
      this.sortBy.set(field);
      this.sortDir.set('asc');
    }
    this.sortMenuOpen.set(false);
    this.pageNumber.set(1);
    this.search();
  }

  /** Clear the YMME sort (back to default newest-first). */
  protected clearSort(): void {
    this.sortBy.set(null);
    this.sortMenuOpen.set(false);
    this.pageNumber.set(1);
    this.search();
  }

  protected readonly yearOptions = computed<ISelectOption[]>(() => [
    { value: '', label: 'All Years' },
    ...this.ymme().years.map((y) => ({ value: y, label: y })),
  ]);
  protected readonly makeOptions = computed<ISelectOption[]>(() => [
    { value: '', label: 'All Makes' },
    ...this.ymme().makes.map((m) => ({ value: m, label: m })),
  ]);
  protected readonly modelOptions = computed<ISelectOption[]>(() => [
    { value: '', label: 'All Models' },
    ...this.ymme().models.map((m) => ({ value: m, label: m })),
  ]);
  protected readonly engineOptions = computed<ISelectOption[]>(() => [
    { value: '', label: 'All Engines' },
    ...this.ymme().engines.map((e) => ({ value: e, label: e })),
  ]);
  protected readonly milOptions: ISelectOption[] = [
    { value: '', label: 'All' },
    { value: '1', label: 'On' },
    { value: '2', label: 'Off' },
    { value: '0', label: 'Unknown' },
  ];

  protected readonly ledOptions: ISelectOption[] = [
    { value: '', label: 'All' },
    { value: '0', label: 'Off' },
    { value: '1', label: 'Green' },
    { value: '2', label: 'Yellow' },
    { value: '3', label: 'Red' },
    { value: '4', label: 'Error' },
  ];

  protected readonly dtcSourceOptions: ISelectOption[] = [
    { value: '', label: 'All' },
    { value: 'ECU', label: 'ECU' },
    { value: 'TCU', label: 'TCU' },
    { value: 'OEM', label: 'OEM' },
  ];

  protected readonly rawBufferOptions: ISelectOption[] = [
    { value: '', label: 'All' },
    { value: 'true', label: 'Has raw buffer' },
    { value: 'false', label: 'No raw buffer' },
  ];

  protected readonly columns: IDataTableColumn[] = [
    { key: 'vin', label: 'VIN', widthClass: 'w-[180px]' },
    { key: 'ymme', label: 'Year / Make / Model / Engine' },
    { key: 'mil', label: 'MIL', align: 'center', widthClass: 'w-[90px]' },
    { key: 'payloadCount', label: 'Payloads', align: 'right', widthClass: 'w-[100px]' },
    { key: 'dtcCount', label: 'DTCs', align: 'right', widthClass: 'w-[90px]' },
    { key: 'rawBufferCount', label: 'Buffers', align: 'right', widthClass: 'w-[90px]' },
    { key: 'actions', label: '', align: 'center', widthClass: 'w-[70px]' },
  ];

  constructor() {
    // Faceted YMME: any Y/M/M/E change reloads all four option lists for valid combinations.
    for (const ctrl of [this.form.controls.year, this.form.controls.make, this.form.controls.model, this.form.controls.engine]) {
      ctrl.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.loadYmmeOptions());
    }

    this.loadYmmeOptions();
    this.search();
  }

  private loadYmmeOptions(): void {
    const v = this.form.getRawValue();
    this.api
      .getYmmeOptions({
        year: v.year || undefined,
        make: v.make || undefined,
        model: v.model || undefined,
        engine: v.engine || undefined,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((options) => this.ymme.set(options));
  }

  /**
   * Minimum time (ms) a loading state stays visible. On dev the API is local and near-instant;
   * without a floor the spinner flashes and users think nothing happened. Keeps feedback
   * perceptible without noticeably slowing real (slower) calls.
   */
  private static readonly MIN_LOADING_MS = 500;

  /** Run `done` once at least MIN_LOADING_MS has elapsed since `startedAt`, so loading stays visible. */
  private runWithMinDelay(startedAt: number, done: () => void): void {
    const remaining = DataLibraryPageComponent.MIN_LOADING_MS - (Date.now() - startedAt);
    if (remaining > 0) {
      setTimeout(done, remaining);
    } else {
      done();
    }
  }

  protected asProfile(row: unknown): IVehicleProfileListItem {
    return row as IVehicleProfileListItem;
  }

  /** ToolMilStatus enum: 0=Unknown, 1=On, 2=Off. */
  protected milLabel(status: number | null): string {
    return status === 1 ? 'On' : status === 2 ? 'Off' : 'Unknown';
  }

  protected milTone(status: number | null): 'error' | 'success' | 'neutral' {
    return status === 1 ? 'error' : status === 2 ? 'success' : 'neutral';
  }

  /** ToolLEDStatus enum: 0=Off, 1=Green, 2=Yellow, 3=Red, 4=Error. */
  protected ledLabel(status: number | null): string {
    return ['Off', 'Green', 'Yellow', 'Red', 'Error'][status ?? -1] ?? '—';
  }

  /**
   * Coerce a year-range control value to a number, or undefined when blank.
   * The `type="number"` inputs bind via NumberValueAccessor, so the value can be a number or null
   * (not the string the form type suggests) — handle all shapes without calling string methods.
   */
  private toYear(value: unknown): number | undefined {
    if (value === null || value === undefined || value === '') {
      return undefined;
    }
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : undefined;
  }

  private buildQuery(): IVehicleProfileSearch {
    const v = this.form.getRawValue();
    return {
      vin: v.vin.trim() || undefined,
      year: v.year || undefined,
      yearFrom: this.toYear(v.yearFrom),
      yearTo: this.toYear(v.yearTo),
      make: v.make || undefined,
      model: v.model || undefined,
      engine: v.engine || undefined,
      sortBy: this.sortBy() ?? undefined,
      sortDirection: this.sortBy() ? this.sortDir() : undefined,
      toolMilStatus: v.mil === '' ? undefined : Number(v.mil),
      toolLedStatus: v.led === '' ? undefined : Number(v.led),
      primaryDtc: v.primaryDtc.trim() || undefined,
      primaryDtcMode: v.primaryDtc.trim() ? (v.primaryDtcMode as DtcMatchMode) : undefined,
      dtcCode: v.dtcCode.trim() || undefined,
      dtcCodeMode: v.dtcCode.trim() ? (v.dtcCodeMode as DtcMatchMode) : undefined,
      dtcSource: v.dtcSource || undefined,
      dtcSystem: v.dtcSystem.trim() || undefined,
      dtcStatusType: v.dtcStatusType.trim() || undefined,
      dtcDefinition: v.dtcDefinition.trim() || undefined,
      dtcFormat: v.dtcFormat || undefined,
      dtcPrefix: v.dtcPrefix || undefined,
      hasRawBuffer: v.hasRawBuffer === '' ? undefined : v.hasRawBuffer === 'true',
      pageNumber: this.pageNumber(),
      pageSize: this.pageSize(),
    };
  }

  private search(): void {
    const startedAt = Date.now();
    this.loading.set(true);
    this.api
      .searchProfiles(this.buildQuery())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (page) =>
          this.runWithMinDelay(startedAt, () => {
            this.result.set(page);
            this.loading.set(false);
          }),
        error: () =>
          this.runWithMinDelay(startedAt, () => {
            this.result.set(DataLibraryPageComponent.emptyPage);
            this.loading.set(false);
          }),
      });
  }

  onSearch(): void {
    this.pageNumber.set(1);
    this.detail.set(null);
    this.selectedId.set(null);
    this.search();
  }

  onReset(): void {
    this.sortBy.set(null);
    this.yearRangeExpanded.set(false);
    this.dtcMoreExpanded.set(false);
    this.form.reset({
      vin: '', year: '', yearFrom: '', yearTo: '', make: '', model: '', engine: '', mil: '', led: '', hasRawBuffer: '',
      primaryDtc: '', primaryDtcMode: 'contains', dtcCode: '', dtcCodeMode: 'contains',
      dtcSource: '', dtcSystem: '', dtcStatusType: '', dtcDefinition: '', dtcFormat: '', dtcPrefix: '',
    });
    this.onSearch();
  }

  onPage(page: number): void {
    this.pageNumber.set(page);
    this.search();
  }

  selectProfile(row: unknown): void {
    const id = this.asProfile(row).id;
    const startedAt = Date.now();
    this.selectedId.set(id);
    this.base64View.set(null);
    this.detail.set(null);
    this.detailLoading.set(true);
    this.api
      .getProfile(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (d) =>
          this.runWithMinDelay(startedAt, () => {
            this.detail.set(d);
            this.detailLoading.set(false);
          }),
        error: () => this.runWithMinDelay(startedAt, () => this.detailLoading.set(false)),
      });
  }

  viewPayload(id: number): void {
    const startedAt = Date.now();
    this.copied.set(false);
    this.base64View.set(null);
    this.base64Loading.set(true);
    this.api
      .getPayload(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (c) =>
          this.runWithMinDelay(startedAt, () => {
            this.base64View.set({ label: c.name || 'Payload #' + c.id, base64: c.base64, byteSize: c.byteSize });
            this.base64Loading.set(false);
          }),
        error: () => this.runWithMinDelay(startedAt, () => this.base64Loading.set(false)),
      });
  }

  viewRawBuffer(id: number): void {
    const startedAt = Date.now();
    this.copied.set(false);
    this.base64View.set(null);
    this.base64Loading.set(true);
    this.api
      .getRawBuffer(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (c) =>
          this.runWithMinDelay(startedAt, () => {
            this.base64View.set({
              label: c.bufferType + (c.segmentIndex !== null ? ' #' + c.segmentIndex : ''),
              base64: c.base64,
              byteSize: c.byteSize,
            });
            this.base64Loading.set(false);
          }),
        error: () => this.runWithMinDelay(startedAt, () => this.base64Loading.set(false)),
      });
  }

  copy(text: string): void {
    void navigator.clipboard?.writeText(text);
    this.copied.set(true);
    setTimeout(() => this.copied.set(false), 1500);
  }

  /**
   * Fetch every raw buffer's content, assemble them into one JSON object
   * ({ BufferType: base64 }, OemModuleRaw as a segment-ordered array), and show it in the
   * base64 viewer so it can be reviewed and copied from there.
   */
  viewBuffersAsJson(d: IVehicleProfileDetail): void {
    if (d.rawBuffers.length === 0 || this.buffersJsonLoading()) {
      return;
    }
    const startedAt = Date.now();
    this.copied.set(false);
    this.base64View.set(null);
    this.buffersJsonLoading.set(true);
    // rawBuffers arrive ordered by (BufferType, SegmentIndex); forkJoin preserves that order,
    // so pushing segments as they come keeps OemModuleRaw in order.
    forkJoin(d.rawBuffers.map((b) => this.api.getRawBuffer(b.id)))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (contents) =>
          this.runWithMinDelay(startedAt, () => {
            const json: Record<string, string | string[] | number | null> = {
              Vin: d.vin,
              UsbProductId: d.usbProductId,
            };
            for (const c of contents) {
              if (c.segmentIndex === null) {
                json[c.bufferType] = c.base64;
              } else {
                const arr = (json[c.bufferType] as string[] | undefined) ?? [];
                arr.push(c.base64);
                json[c.bufferType] = arr;
              }
            }
            this.base64View.set({ label: 'Raw Buffers (JSON)', base64: JSON.stringify(json, null, 2), byteSize: null });
            this.buffersJsonLoading.set(false);
          }),
        error: () => this.runWithMinDelay(startedAt, () => this.buffersJsonLoading.set(false)),
      });
  }
}
