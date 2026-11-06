import { Component, ElementRef, HostListener, OnInit, ViewChild, ViewEncapsulation, output, signal, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { finalize } from 'rxjs';

import {
  ButtonComponent,
  ISelectOption,
  InputComponent,
  SelectComponent,
  TextareaComponent,
  ToggleComponent,
} from '@shared/components';

import { ComboOption, ComboSelectComponent } from '../combo-select';
import { FfComboSelectComponent } from '../ff-combo-select';
import {
  BUFFER_NAME_OPTIONS,
  ECU_DTC_TYPE_OPTIONS,
  INNOVA_GROUP_COMBO_OPTIONS,
} from '../../constants/payload-enums.const';
import { OFMItemDef, OFMItemsService } from '../../services/ofm-items.service';
import { OemSystemEnumsService } from '../../services/oem-system-enums.service';
import {
  DtcEntry,
  FreezeFrameItemDef,
  FreezeFrameItemInput,
  GenerateRawPayloadRequest,
  GenerateVehicleBuffersRequest,
  MonitorInput,
  OFMItemInput,
  OemDtcInput,
  PayloadGeneratorService,
  RecordTimeInput,
  VinProfileInput,
} from '../../services/payload-generator.service';
import { VinEnumsService } from '../../services/vin-enums.service';

/**
 * Payload Generator modal (opened from the Data Library page). Users configure a vehicle payload
 * (VIN / profile / monitors / freeze frames / DTCs / OEM / OFM) and generate either a full raw
 * payload or per-segment raw buffers by calling the deployed generator API, then copy the result —
 * without leaving the site. Styled with the app's design tokens.
 *
 * ViewEncapsulation.None + a `.pg-root` wrapper: the form's shared classes (.card/.input/…) are
 * scoped under `.pg-root` so they also reach the child combo-select inputs without leaking.
 */
@Component({
  selector: 'app-payload-generator-modal',
  standalone: true,
  imports: [
    FormsModule,
    MatIconModule,
    InputComponent,
    SelectComponent,
    TextareaComponent,
    ToggleComponent,
    ButtonComponent,
    ComboSelectComponent,
    FfComboSelectComponent,
  ],
  templateUrl: './payload-generator-modal.component.html',
  styleUrl: './payload-generator-modal.component.scss',
  encapsulation: ViewEncapsulation.None,
})
export class PayloadGeneratorModalComponent implements OnInit {
  /** Emitted when the user dismisses the modal (backdrop / ✕ / Escape). */
  readonly closed = output<void>();

  @ViewChild('resultSection') resultSection?: ElementRef<HTMLElement>;

  close(): void {
    this.closed.emit();
  }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    if (!this.showImportModal()) {
      this.close();
    }
  }

  private scrollToResult() {
    setTimeout(
      () => this.resultSection?.nativeElement?.scrollIntoView({ behavior: 'smooth', block: 'nearest' }),
      50,
    );
  }

  /**
   * Minimum time (ms) a loading state stays visible. On dev the generator API is local and
   * responds almost instantly; without a floor the spinner would flash and users could think
   * nothing happened. Keeps feedback perceptible without noticeably slowing real (slower) calls.
   */
  private static readonly MIN_LOADING_MS = 600;

  /** Run `done` once at least MIN_LOADING_MS has elapsed since `startedAt`, so loading stays visible. */
  private runWithMinDelay(startedAt: number, done: () => void): void {
    const remaining = PayloadGeneratorModalComponent.MIN_LOADING_MS - (Date.now() - startedAt);
    if (remaining > 0) {
      setTimeout(done, remaining);
    } else {
      done();
    }
  }

  // ─── Initial option lists loading (VIN / OEM system / freeze-frame / OFM enums) ───
  /** True until every enum/option list has finished loading, so combos show a loading hint. */
  readonly optionsLoading = signal(true);
  private pendingOptionLoads = 4;
  private markOptionLoadDone(): void {
    if (--this.pendingOptionLoads <= 0) {
      this.optionsLoading.set(false);
    }
  }

  // Tab state
  activeTab: 'buffers' | 'combine' = 'buffers';

  // ─── Tab: Combine OEM Module ───
  combineOemRawInput = '';
  combineVinProfileRawInput = '';
  combineResult = signal('');
  combineLoading = signal(false);
  combineCopied = signal(false);

  combineOemModule() {
    let lines: string[];
    try {
      const parsed = JSON.parse(this.combineOemRawInput);
      lines = Array.isArray(parsed) ? parsed.filter((s: string) => s) : [];
    } catch {
      this.combineResult.set('Error: Invalid JSON array');
      return;
    }
    if (lines.length === 0) return;

    const startedAt = Date.now();
    this.combineLoading.set(true);
    this.combineResult.set('');
    this.payloadService
      .combineOemModule(lines, this.combineVinProfileRawInput.trim() || undefined)
      .subscribe({
        next: (res) =>
          this.runWithMinDelay(startedAt, () => {
            this.combineResult.set(res.oemModuleRawPayload ?? '');
            this.combineLoading.set(false);
          }),
        error: (err) =>
          this.runWithMinDelay(startedAt, () => {
            this.combineResult.set('Error: ' + (err.error?.message || err.message));
            this.combineLoading.set(false);
          }),
      });
  }

  copyCombineResult() {
    this.writeClipboard(this.combineResult());
    this.combineCopied.set(true);
    setTimeout(() => this.combineCopied.set(false), 2000);
  }

  // ─── Import Payload sub-modal ───
  showImportModal = signal(false);
  importPayloadText = '';
  importLoading = signal(false);
  importError = signal('');
  importedBasePayload = signal('');

  openImportModal() {
    this.importPayloadText = '';
    this.importError.set('');
    this.showImportModal.set(true);
  }

  closeImportModal() {
    this.showImportModal.set(false);
    this.importError.set('');
  }

  importPayload() {
    const raw = this.importPayloadText.trim();
    if (!raw) return;
    const startedAt = Date.now();
    this.importLoading.set(true);
    this.importError.set('');
    this.payloadService.parsePayload(raw).subscribe({
      next: (data) =>
        this.runWithMinDelay(startedAt, () => {
          this.applyParsedPayload(data);
          this.importedBasePayload.set(raw);
          this.importLoading.set(false);
          this.showImportModal.set(false);
          this.importPayloadText = '';
        }),
      error: (err) =>
        this.runWithMinDelay(startedAt, () => {
          this.importError.set('Error: ' + (err.error?.message || err.message));
          this.importLoading.set(false);
        }),
    });
  }

  clearImportMode() {
    this.importedBasePayload.set('');
  }

  private applyParsedPayload(data: GenerateRawPayloadRequest) {
    // Basic settings
    if (data.Vin) this.vin = data.Vin;
    if (data.UsbProductId != null) this.usbProductId = data.UsbProductId;

    // Payload settings — bypass setters to avoid spurious sync before monitors/ff are set
    if (data.OBD2_MIL != null) this._obd2Mil = data.OBD2_MIL;
    if (data.FF_DTC) this._ffDtc = data.FF_DTC;
    if (data.LedStatus) this.ledStatus = data.LedStatus;
    if (data.SIL) this.sil = data.SIL;
    this.absLightStatus = data.ABSLightStatus ?? '';
    this.tpmsLightStatus = data.TPMSLightStatus ?? '';

    // VinProfile
    if (data.VinProfile) {
      this.vinProfile = {
        Manufacturer: data.VinProfile.Manufacturer ?? 12,
        Year: data.VinProfile.Year ?? 33,
        Make: data.VinProfile.Make ?? 29,
        Model: data.VinProfile.Model ?? 4,
        Trim: data.VinProfile.Trim ?? 65535,
        BodyCode: data.VinProfile.BodyCode ?? 65535,
        Engine: data.VinProfile.Engine ?? 48,
        Transmission: data.VinProfile.Transmission ?? 0,
        MakeGroup: data.VinProfile.MakeGroup ?? 0,
        VehID: data.VinProfile.VehID ?? 0,
      };
    }

    // Monitors — set first, then sync obd2Mil from ECU MIL entry (matches backend ParsePayload)
    this.ecuMonitors = data.ECU_Monitors?.map((m) => ({ Name: m.Name, Key: m.Key })) ?? [];
    this.tcuMonitors = data.TCU_Monitors?.map((m) => ({ Name: m.Name, Key: m.Key })) ?? [];
    const ecuMilMonitor = this.ecuMonitors.find((m) => m.Name === this.MONITOR_NAMES[0]);
    if (ecuMilMonitor) this._obd2Mil = ecuMilMonitor.Key === 1;

    // Freeze Frame — attach _label for the combo-select display
    this.ecuFreezeFrame = (data.ECU_FreezeFrame ?? []).map((ff) => {
      const opt = this.freezeFrameOptions.find(
        (o) => o.name === ff.Name && (!ff.Pid || o.pid === ff.Pid),
      );
      return { ...ff, _label: opt?.label ?? ff.Name } as FreezeFrameItemInput;
    });
    this.tcuFreezeFrame = (data.TCU_FreezeFrame ?? []).map((ff) => {
      const opt = this.freezeFrameOptions.find(
        (o) => o.name === ff.Name && (!ff.Pid || o.pid === ff.Pid),
      );
      return { ...ff, _label: opt?.label ?? ff.Name } as FreezeFrameItemInput;
    });
    // Sync ffDtc from ECU Freeze Frame DTC entry if present
    const ffDtcEntry = this.ecuFreezeFrame.find((ff) => ff.Name === this.FF_DTC_NAME);
    if (ffDtcEntry?.Values[0]) this._ffDtc = ffDtcEntry.Values[0];

    // ECU + TCU DTCs
    this.bufEcuDtcs = data.ECU_DTCs?.map((d) => ({ code: d.Code, type: d.Type })) ?? [];
    this.bufTcuDtcs = data.BufTCU_DTCs?.map((d) => ({ code: d.Code, type: d.Type })) ?? [];

    // OEM DTCs (shared)
    this.oemDtcs = data.OEM_DTCs?.map((d) => ({ ...d })) ?? [];
    // Sync shared make + option lists from first imported DTC (all imports share one make).
    const importedMake = this.oemDtcs[0]?.MakeEnum;
    if (importedMake != null && importedMake !== this.oemMakeEnum) {
      this.oemMakeEnum = importedMake;
      this.oemSystemOptions = this._systemByMake.get(importedMake) ?? [];
      this.oemSubSystemOptions = this._subsystemByMake.get(importedMake) ?? [];
      this.ofmMakeEnum = importedMake;
      if (this._ofmDefs.length > 0)
        this.ofmItemOptions = this.ofmItemsService.getOptionsForMake(this._ofmDefs, importedMake);
    }

    // Advanced Basic Settings
    if (data.BatteryConditionStatus) this.batteryConditionStatus = data.BatteryConditionStatus;
    if (data.OdometerValue != null) this.odometerValue = data.OdometerValue;
    if (data.OdometerUnit) this.odometerUnit = data.OdometerUnit;
    if (data.BatteryVoltage) {
      const numStr = data.BatteryVoltage.replace(/[^\d.-]/g, '').trim();
      const parsed = parseFloat(numStr);
      this.batteryVoltageValue = isFinite(parsed) ? parsed : null;
    }
    if (data.RecordTime) Object.assign(this.recordTime, data.RecordTime);

    // OFM Items
    if (data.OFM?.Items?.length) {
      this.ofmItems = data.OFM.Items.map((i) => ({ ...i }));
    }
  }

  // Basic settings (shared)
  vin = '1N4BL3AP5DC125177';
  usbProductId: number | null = 1688; // 0x698

  // ─── Advanced Basic Settings (collapsible) ───
  basicSettingsExpanded = false;

  // ─── Collapsible secondary sections (collapsed by default; focus stays on VIN + DTCs) ───
  showVinProfile = false;
  showMonitors = false;
  showFreezeFrame = false;
  showTcuDtcs = false;
  showOfm = false;

  batteryConditionStatus = 'Battery status unknown';
  odometerValue: number | null = 42892;
  odometerUnit = 'miles';
  batteryVoltageValue: number | null = 11.65;

  recordTime: RecordTimeInput = {
    OBD2: 7266,
    ABS: 1404,
    TCM: 1929,
    ODO: 2381,
    ECM_PCM: null,
    SIL: 1668,
    ABS_LIGHT: 1017,
    OFM_ITEMS: 11514,
    TPMS: 2346,
    TPMS_LIGHT: 3307,
  };

  // ─── OFM Items ───
  ofmMakeEnum = 0;
  ofmItems: OFMItemInput[] = [];

  private _ofmDefs: OFMItemDef[] = [];
  ofmItemOptions: ComboOption[] = [];

  addOfmItem() {
    this.ofmItems.unshift({ ItemId: 0, ItemName: '', Value: '', Unit: '', ItemDescriptionEnum: 0 });
  }
  removeOfmItem(i: number) {
    this.ofmItems.splice(i, 1);
  }

  onOfmItemSelect(item: OFMItemInput, itemId: number) {
    // Try direct lookup (covers base items in DB).
    const direct = this._ofmDefs.find((d) => d.makeId === this.ofmMakeEnum && d.itemId === itemId);
    if (direct) {
      item.ItemId = direct.itemId;
      item.ItemName = direct.itemName;
      item.ItemDescriptionEnum = direct.itemDescriptionEnum;
      if (direct.unit !== undefined) item.Unit = direct.unit;
      return;
    }
    // TPMS Status variant: id has bit 0x8000 set; base itemid = id & 0x7FFF,
    // name = base + " Status", descriptionEnum from EnumTireStatus map.
    if ((itemId & 0x8000) !== 0) {
      const baseId = itemId & 0x7fff;
      const baseDef = this._ofmDefs.find((d) => d.makeId === this.ofmMakeEnum && d.itemId === baseId);
      if (baseDef) {
        const statusName = `${baseDef.itemName} Status`;
        const statusEnum = this.ofmItemsService.getTpmsStatusEnum(statusName);
        if (statusEnum != null) {
          item.ItemId = itemId;
          item.ItemName = statusName;
          item.ItemDescriptionEnum = statusEnum;
          item.Unit = ''; // status is a string ("OK"/etc) — no unit
          return;
        }
      }
    }
    // Unknown id (e.g. imported variant not yet covered) — keep id, leave name/desc as-is.
    item.ItemId = itemId;
  }

  private _obd2Mil = true;
  get obd2Mil() {
    return this._obd2Mil;
  }
  set obd2Mil(value: boolean) {
    this._obd2Mil = value;
    this.syncMilToMonitors(value);
  }

  private _ffDtc = 'P0420';
  get ffDtc() {
    return this._ffDtc;
  }
  set ffDtc(value: string) {
    this._ffDtc = value;
    this.syncFfDtcToFreezeFrame(value);
  }

  ledStatus = 'Red';
  sil = 'ON';
  absLightStatus = 'ON';
  tpmsLightStatus = 'OFF';

  // ─── OEM DTCs (shared, below basic settings) ───
  oemDtcs: OemDtcInput[] = [];
  oemMakeEnum = 0; // shared make for all OEM DTCs
  oemMakeOptions: ComboOption[] = [];
  oemSystemOptions: ComboOption[] = [];
  oemSubSystemOptions: ComboOption[] = [];
  private _systemByMake = new Map<number, ComboOption[]>();
  private _subsystemByMake = new Map<number, ComboOption[]>();
  private _defaultSystem = 0;
  private _defaultSubSystem = 65535;
  groupOptions: ComboOption[] = INNOVA_GROUP_COMBO_OPTIONS;

  /** Most-picked OEM DTC types, surfaced first so users find them quickly. */
  private static readonly COMMON_OEM_TYPES: readonly string[] = [
    'Confirmed',
    'Current',
    'Pending',
    'Permanent',
    'Active',
    'Stored',
    'History',
    'Present',
    'Past',
    'MIL',
    'Intermittent',
    'Test Failed',
  ];

  /** OEM DTC type chips: common types first, then the rest (original order preserved). */
  readonly bufferNameOptions: string[] = [
    ...PayloadGeneratorModalComponent.COMMON_OEM_TYPES.filter((t) => (BUFFER_NAME_OPTIONS as readonly string[]).includes(t)),
    ...BUFFER_NAME_OPTIONS.filter((t) => !PayloadGeneratorModalComponent.COMMON_OEM_TYPES.includes(t)),
  ];

  /** Count of common types shown first (for the visual divider in the chip list). */
  protected readonly commonTypeCount = PayloadGeneratorModalComponent.COMMON_OEM_TYPES.filter((t) =>
    (BUFFER_NAME_OPTIONS as readonly string[]).includes(t),
  ).length;

  addOemDtc() {
    this.oemDtcs.unshift({
      MakeEnum: this.oemMakeEnum,
      System: this._defaultSystem,
      SubSystem: this._defaultSubSystem,
      Code: '',
      Group: 0x02, // ABS
      Types: [],
    });
  }
  removeOemDtc(i: number) {
    this.oemDtcs.splice(i, 1);
  }
  onOemMakeChange(makeEnum: number) {
    this.oemMakeEnum = makeEnum;
    this.oemSystemOptions = this._systemByMake.get(makeEnum) ?? [];
    this.oemSubSystemOptions = this._subsystemByMake.get(makeEnum) ?? [];
    const defaultSystem = this.oemSystemOptions[0]?.id ?? 0;
    const defaultSubSystem = this.oemSubSystemOptions[0]?.id ?? 65535;
    this._defaultSystem = defaultSystem;
    this._defaultSubSystem = defaultSubSystem;
    // Propagate make to all existing DTCs and reset their System/SubSystem to defaults
    for (const dtc of this.oemDtcs) {
      dtc.MakeEnum = makeEnum;
      dtc.System = defaultSystem;
      dtc.SubSystem = defaultSubSystem;
    }
    // Sync OFM make options to match
    this.ofmMakeEnum = makeEnum;
    if (this._ofmDefs.length > 0)
      this.ofmItemOptions = this.ofmItemsService.getOptionsForMake(this._ofmDefs, makeEnum);
  }
  toggleOemType(dtc: OemDtcInput, type: string) {
    dtc.Types = dtc.Types[0] === type ? [] : [type];
  }

  ecuDtcTypeOptions = ECU_DTC_TYPE_OPTIONS;

  // ─── Option lists for the shared app-select dropdowns ───
  protected readonly ledSelectOptions: ISelectOption[] = [
    { value: 'Red', label: 'Red' },
    { value: 'Yellow', label: 'Yellow' },
    { value: 'Green', label: 'Green' },
  ];
  protected readonly silSelectOptions: ISelectOption[] = [
    { value: 'ON', label: 'ON' },
    { value: 'OFF', label: 'OFF' },
  ];
  protected readonly lightSelectOptions: ISelectOption[] = [
    { value: '', label: '— Empty —' },
    { value: 'ON', label: 'ON' },
    { value: 'OFF', label: 'OFF' },
    { value: 'Not Supported', label: 'Not Supported' },
  ];
  protected readonly batterySelectOptions: ISelectOption[] = [
    { value: 'Battery status unknown', label: 'Battery status unknown' },
    { value: 'Battery Good', label: 'Battery Good' },
    { value: 'Battery Warning', label: 'Battery Warning' },
    { value: 'Battery Bad', label: 'Battery Bad' },
    { value: 'Battery Not Detect', label: 'Battery Not Detect' },
    { value: 'Battery Low', label: 'Battery Low' },
  ];
  protected readonly odoUnitSelectOptions: ISelectOption[] = [
    { value: 'miles', label: 'miles' },
    { value: 'km', label: 'km' },
  ];
  protected readonly dtcTypeSelectOptions: ISelectOption[] = ECU_DTC_TYPE_OPTIONS.map((t) => ({ value: t, label: t }));

  // ─── Vehicle Buffers ───

  // VinProfile fields
  vinProfile: VinProfileInput = {
    Manufacturer: 12,
    Year: 33,
    Make: 29,
    Model: 4,
    Trim: 65535,
    BodyCode: 65535,
    Engine: 48,
    Transmission: 0,
    MakeGroup: 0,
    VehID: 0,
  };

  // Monitor constants
  readonly MONITOR_NAMES = [
    'Malfunction Indicator Lamp (MIL)',
    'Comprehensive Component Monitoring',
    'Fuel System Monitoring',
    'Misfire Monitoring',
    'Catalyst Monitoring',
    'Heated Catalyst Monitoring',
    'Evaporative System Monitoring',
    'Secondary Air System Monitoring',
    'Oxygen Sensor Monitoring',
    'Oxygen Sensor Heater Monitoring',
    'EGR and/or VVT System Monitoring',
  ] as const;

  readonly MONITOR_KEY_OPTIONS = [
    { value: 0, label: 'OFF (MIL)' },
    { value: 1, label: 'ON (MIL)' },
    { value: 2, label: 'Not Supported' },
    { value: 3, label: 'Not Complete' },
    { value: 4, label: 'Complete / Ready' },
  ] as const;

  // Freeze frame name+unit list — loaded from API
  freezeFrameDefs: FreezeFrameItemDef[] = [];
  freezeFrameOptions: {
    label: string;
    description: string;
    name: string;
    unit: string;
    pid: number;
  }[] = [];

  // ECU data
  ecuMonitors: MonitorInput[] = [];
  ecuFreezeFrame: FreezeFrameItemInput[] = [];
  bufEcuDtcs: { code: string; type: string }[] = [{ code: 'P0420', type: 'Stored' }];

  // TCU data
  tcuMonitors: MonitorInput[] = [];
  tcuFreezeFrame: FreezeFrameItemInput[] = [];
  bufTcuDtcs: { code: string; type: string }[] = [];

  // ECU Monitors
  addEcuMonitor() {
    this.ecuMonitors.unshift({ Name: this.MONITOR_NAMES[0], Key: 4 });
  }
  removeEcuMonitor(i: number) {
    const wasMil = this.ecuMonitors[i].Name === this.MONITOR_NAMES[0];
    this.ecuMonitors.splice(i, 1);
    if (wasMil) this.refreshMilToggle();
  }

  // TCU Monitors
  addTcuMonitor() {
    this.tcuMonitors.unshift({ Name: this.MONITOR_NAMES[0], Key: 4 });
  }
  removeTcuMonitor(i: number) {
    const wasMil = this.tcuMonitors[i].Name === this.MONITOR_NAMES[0];
    this.tcuMonitors.splice(i, 1);
    if (wasMil) this.refreshMilToggle();
  }

  // Re-derive obd2Mil from current monitor arrays (called after add/remove)
  private refreshMilToggle() {
    const ecuMil = this.ecuMonitors.find((m) => m.Name === this.MONITOR_NAMES[0]);
    const tcuMil = this.tcuMonitors.find((m) => m.Name === this.MONITOR_NAMES[0]);
    this._obd2Mil = ecuMil?.Key === 1 || tcuMil?.Key === 1;
  }

  // ECU Freeze Frame
  addEcuFreezeFrameItem() {
    this.ecuFreezeFrame.unshift({ Name: '', Unit: '', Values: [''], _label: '' } as FreezeFrameItemInput);
  }
  removeEcuFreezeFrameItem(i: number) {
    this.ecuFreezeFrame.splice(i, 1);
  }

  // TCU Freeze Frame
  addTcuFreezeFrameItem() {
    this.tcuFreezeFrame.unshift({ Name: '', Unit: '', Values: [''], _label: '' } as FreezeFrameItemInput);
  }
  removeTcuFreezeFrameItem(i: number) {
    this.tcuFreezeFrame.splice(i, 1);
  }

  // Freeze frame label input handler — fires on every keystroke; sets Name/Unit/Pid on exact match
  onFreezeFrameLabelInput(ff: FreezeFrameItemInput) {
    const label = ((ff as FreezeFrameItemInput & { _label: string })._label ?? '').trim();
    const match = this.freezeFrameOptions.find((o) => o.label === label);
    if (match) {
      ff.Name = match.name;
      ff.Unit = match.unit;
      ff.Pid = match.pid;
    } else if (!label) {
      ff.Name = '';
      ff.Pid = undefined;
    }
  }

  // Freeze frame values management
  addFreezeFrameValue(ff: FreezeFrameItemInput) {
    ff.Values.push('');
  }
  removeFreezeFrameValue(ff: FreezeFrameItemInput, vi: number) {
    ff.Values.splice(vi, 1);
  }
  trackByIndex(index: number) {
    return index;
  }

  // ECU DTCs for buffers
  addBufEcuDtc() {
    this.bufEcuDtcs.unshift({ code: '', type: 'Stored' });
  }
  removeBufEcuDtc(i: number) {
    this.bufEcuDtcs.splice(i, 1);
  }

  // TCU DTCs for buffers
  addBufTcuDtc() {
    this.bufTcuDtcs.unshift({ code: '', type: 'Stored' });
  }
  removeBufTcuDtc(i: number) {
    this.bufTcuDtcs.splice(i, 1);
  }

  // Code validation
  // SAE digit1 must be 0-3 (2-bit field in byte0[5:4]); digit1 >= 4 overflows into category bits
  private ecuCodeRegex = /^[PCBUpbcu][0-3][0-9A-Fa-f]{3}$/;
  isValidEcuCode(code: string): boolean {
    return this.ecuCodeRegex.test(code);
  }
  // SAE: [PCBU][0-3][hex]{3} for 2-byte SAE; extra hex pairs for 3+ byte SAE
  // (e.g. type-28 with formatDtc=3 produces 7-char codes like "U021200").
  // Optional qualifier: 1-3 hex chars (covers types 23-26 hex or 3-digit decimal sub).
  private saeOemRegex = /^[PCBUpbcu][0-3][0-9A-Fa-f]{3}(?:[0-9A-Fa-f]{2})*(?:-[0-9A-Fa-f]{1,3})?$/;
  // Non-SAE: 1-8 hex chars + optional dash + (1-2 hex | 000-255 decimal)
  private nonSaeOemRegex =
    /^[0-9A-Fa-f]{1,8}(?:-(?:[0-9A-Fa-f]{1,2}|(?:[01][0-9]{2}|2[0-4][0-9]|25[0-5])))?$/;
  private static readonly SPECIAL_OEM_CODES = ['no code', 'available'];
  isValidOemCode(code: string): boolean {
    if (PayloadGeneratorModalComponent.SPECIAL_OEM_CODES.includes(code.toLowerCase())) return true;
    return this.saeOemRegex.test(code) || this.nonSaeOemRegex.test(code);
  }
  // Normalize OEM code for roundtrip fidelity with toolInformationResolver.Parse().
  // Simulates the DLL's decode behavior so that encode → Parse → Code == normalized input.
  normalizeOemCode(code: string): string {
    // Special codes: preserve canonical casing
    const lower = code.toLowerCase();
    if (lower === 'no code') return 'No code';
    if (lower === 'available') return 'Available';
    // SAE codes: just uppercase (DLL preserves SAE format exactly)
    if (this.saeOemRegex.test(code)) return code.toUpperCase();

    const dIdx = code.indexOf('-');
    if (dIdx === -1) {
      // Hex plain
      const upper = code.toUpperCase();
      const len = upper.length;
      // Even-length 2/4/6: types 8/10/12 preserve leading zeros via PadLeft
      if (len === 2 || len === 4 || len === 6) return upper;
      // Odd-length or 8 chars: type 7 uses .ToString("X") → strip leading zeros
      return this.stripLeadingZeros(upper);
    }

    const main = code.substring(0, dIdx).toUpperCase();
    const sub = code.substring(dIdx + 1);

    // Decimal sub (3 digits, 000-255): type 20 — main uses .ToString("X") (stripped)
    if (/^\d{3}$/.test(sub) && parseInt(sub, 10) <= 255)
      return this.stripLeadingZeros(main) + '-' + sub;

    // Hex sub
    const subUpper = sub.toUpperCase();
    const anyOdd = main.length % 2 !== 0 || subUpper.length % 2 !== 0;
    if (anyOdd) {
      // Type 21: .ToString("X") both sides → strip leading zeros
      return this.stripLeadingZeros(main) + '-' + this.stripLeadingZeros(subUpper);
    }
    // Type 22: PadLeft preserves leading zeros → keep as-is
    return main + '-' + subUpper;
  }
  private stripLeadingZeros(hex: string): string {
    const stripped = hex.replace(/^0+/, '');
    return stripped || '0';
  }
  readonly FF_DTC_NAME = 'DTC that caused required freeze frame data storage';
  isFfDtcEntry(name: string): boolean {
    return name === this.FF_DTC_NAME;
  }
  hasFfDtcValueErrors(ff: { Name: string; Values: string[] }): boolean {
    return this.isFfDtcEntry(ff.Name) && ff.Values.some((v) => v && !this.isValidEcuCode(v));
  }
  hasValidationErrors(): boolean {
    if (this.ffDtc && !this.isValidEcuCode(this.ffDtc)) return true;
    if (this.oemDtcs.some((d) => d.Code && !this.isValidOemCode(d.Code))) return true;
    if (this.bufEcuDtcs.some((d) => d.code && !this.isValidEcuCode(d.code))) return true;
    if (this.bufTcuDtcs.some((d) => d.code && !this.isValidEcuCode(d.code))) return true;
    const ffDtcInvalid = (ff: { Name: string; Values: string[] }) =>
      this.isFfDtcEntry(ff.Name) && ff.Values.some((v) => v && !this.isValidEcuCode(v));
    if (this.ecuFreezeFrame.some(ffDtcInvalid)) return true;
    if (this.tcuFreezeFrame.some(ffDtcInvalid)) return true;
    return false;
  }

  // Sync OBD2 MIL toggle → both ECU and TCU Monitors MIL entry (mirrors backend ApplyModifications)
  private syncMilToMonitors(milOn: boolean) {
    const key = milOn ? 1 : 0;
    for (const monitors of [this.ecuMonitors, this.tcuMonitors]) {
      const existing = monitors.find((m) => m.Name === this.MONITOR_NAMES[0]);
      if (existing) {
        existing.Key = key;
      } else {
        monitors.push({ Name: this.MONITOR_NAMES[0], Key: key });
      }
    }
  }

  // Sync Freeze Frame DTC field → ECU Freeze Frame DTC entry
  private syncFfDtcToFreezeFrame(dtc: string) {
    const idx = this.ecuFreezeFrame.findIndex((ff) => ff.Name === this.FF_DTC_NAME);
    if (!dtc) {
      if (idx !== -1) this.ecuFreezeFrame.splice(idx, 1);
      return;
    }
    const existing = idx !== -1 ? this.ecuFreezeFrame[idx] : null;
    if (existing) {
      if (existing.Values.length === 0) existing.Values.push(dtc);
      else existing.Values[0] = dtc;
    } else if (dtc) {
      const entry = {
        Name: this.FF_DTC_NAME,
        Unit: ' ',
        Values: [dtc],
        Pid: 0x0202,
      } as FreezeFrameItemInput;
      (entry as FreezeFrameItemInput & { _label: string })._label = this.FF_DTC_NAME;
      this.ecuFreezeFrame.unshift(entry);
    }
  }

  // ECU Monitor key change → sync back to OBD2 MIL toggle
  onMonitorKeyChange(m: MonitorInput, value: number) {
    m.Key = value;
    if (m.Name === this.MONITOR_NAMES[0]) {
      this._obd2Mil = value === 1;
    }
  }

  // ECU Freeze Frame value change → sync back to Freeze Frame DTC field
  onFreezeFrameValueChange(ff: FreezeFrameItemInput, index: number, value: string) {
    ff.Values[index] = value;
    if (this.isFfDtcEntry(ff.Name) && index === 0) {
      this._ffDtc = ff.Values[0] ?? '';
    }
  }

  // Result
  loading = signal(false);
  result = signal('');
  copied = signal(false);
  lastGenerated = signal<'payload' | 'buffers' | ''>('');
  generatingFor = signal<'payload' | 'buffers' | ''>('');

  // VinProfile enum options (for combobox dropdowns)
  mfrOptions: ComboOption[] = [];
  yearOptions: ComboOption[] = [];
  makeOptions: ComboOption[] = [];
  modelOptions: ComboOption[] = [];
  trimOptions: ComboOption[] = [];
  engineOptions: ComboOption[] = [];
  transmissionOptions: ComboOption[] = [];

  private readonly payloadService = inject(PayloadGeneratorService);
  private readonly vinEnumsService = inject(VinEnumsService);
  private readonly oemSystemEnumsService = inject(OemSystemEnumsService);
  private readonly ofmItemsService = inject(OFMItemsService);

  ngOnInit(): void {
    this.loadVinEnums();
    this.loadOemSystemEnums();
    this.loadFreezeFrameItems();
    this.loadOFMItems();
    // Sync initial defaults so UI is consistent on first load
    this.syncMilToMonitors(this._obd2Mil);
    this.syncFfDtcToFreezeFrame(this._ffDtc);
    this.ecuFreezeFrame.push({ Name: 'Fuel Sys 1', Unit: '', Values: ['OL'], Pid: 0x03, _label: 'Fuel Sys 1' } as FreezeFrameItemInput);
    this.ecuFreezeFrame.push({ Name: 'Fuel Sys 2', Unit: '', Values: ['OL'], Pid: 0x03, _label: 'Fuel Sys 2' } as FreezeFrameItemInput);
  }

  private loadVinEnums(): void {
    this.vinEnumsService.getAll().pipe(finalize(() => this.markOptionLoadDone())).subscribe((data) => {
      this.mfrOptions = data.manufacturer;
      this.yearOptions = data.year;
      this.makeOptions = data.make;
      this.modelOptions = data.model;
      this.trimOptions = data.trim;
      this.engineOptions = data.engine;
      this.transmissionOptions = data.transmission;
    });
  }

  private loadOemSystemEnums(): void {
    this.oemSystemEnumsService.getAll().pipe(finalize(() => this.markOptionLoadDone())).subscribe((data) => {
      this.oemMakeOptions = data.makes;

      // Group system entries by makeId
      for (const s of data.system) {
        let arr = this._systemByMake.get(s.makeId);
        if (!arr) {
          arr = [];
          this._systemByMake.set(s.makeId, arr);
        }
        arr.push({ id: s.id, name: s.name });
      }

      // Group subsystem entries by makeId
      for (const s of data.subsystem) {
        let arr = this._subsystemByMake.get(s.makeId);
        if (!arr) {
          arr = [];
          this._subsystemByMake.set(s.makeId, arr);
        }
        arr.push({ id: s.id, name: s.name });
      }

      // Set defaults from first make
      const firstMake = data.makes[0];
      if (firstMake) {
        this.oemMakeEnum = firstMake.id;
        this.oemSystemOptions = this._systemByMake.get(firstMake.id) ?? [];
        this.oemSubSystemOptions = this._subsystemByMake.get(firstMake.id) ?? [];
        this._defaultSystem = this.oemSystemOptions[0]?.id ?? 0;
        this._defaultSubSystem = this.oemSubSystemOptions[0]?.id ?? 65535;
        // Sync OFM make and rebuild options if defs already loaded
        this.ofmMakeEnum = firstMake.id;
        if (this._ofmDefs.length > 0)
          this.ofmItemOptions = this.ofmItemsService.getOptionsForMake(this._ofmDefs, firstMake.id);
      }
    });
  }

  private loadOFMItems(): void {
    this.ofmItemsService.getAll().pipe(finalize(() => this.markOptionLoadDone())).subscribe((defs) => {
      this._ofmDefs = defs;
      // Rebuild options if a make is already selected (oemSystemEnums loaded first)
      if (this.ofmMakeEnum > 0)
        this.ofmItemOptions = this.ofmItemsService.getOptionsForMake(defs, this.ofmMakeEnum);
    });
  }

  private loadFreezeFrameItems(): void {
    this.payloadService.getFreezeFrameItems().pipe(finalize(() => this.markOptionLoadDone())).subscribe((defs) => {
      this.freezeFrameDefs = defs;
      // Build dropdown options: deduplicate by (name, pid) and show PID hex for duplicate names
      const nameCount = new Map<string, number>();
      for (const d of defs) nameCount.set(d.name, (nameCount.get(d.name) || 0) + 1);

      const seen = new Set<string>();
      // Prepend the special DTC freeze frame entry (PID 0x0202)
      this.freezeFrameOptions = [
        {
          label: this.FF_DTC_NAME,
          description: 'PID 0x0202 — DTC that caused required freeze frame data storage',
          name: this.FF_DTC_NAME,
          unit: ' ',
          pid: 0x0202,
        },
      ];
      for (const d of defs) {
        const key = `${d.name}|${d.pid}`;
        if (seen.has(key)) continue;
        seen.add(key);
        const isDup = (nameCount.get(d.name) || 0) > 1;
        // For items sharing the same name AND pid (multiple byte positions), show only once
        const label = isDup
          ? `${d.name} (PID 0x${d.pid.toString(16).toUpperCase().padStart(2, '0')})`
          : d.name;
        this.freezeFrameOptions.push({
          label,
          description: d.description,
          name: d.name,
          unit: d.unit,
          pid: d.pid,
        });
      }
    });
  }

  generatePayload() {
    const req: GenerateRawPayloadRequest = {
      Vin: this.vin || undefined,
      UsbProductId: this.usbProductId ?? undefined,
      OBD2_MIL: this.obd2Mil,
      FF_DTC: this.ffDtc || undefined,
      LedStatus: this.ledStatus,
      SIL: this.sil,
      ABSLightStatus: this.absLightStatus ?? undefined,
      TPMSLightStatus: this.tpmsLightStatus ?? undefined,
      BasePayload: this.importedBasePayload() || undefined,
      // Advanced Basic Settings (non-UI segments)
      BatteryConditionStatus: this.batteryConditionStatus || undefined,
      OdometerValue: this.odometerValue ?? undefined,
      OdometerUnit: this.odometerUnit || undefined,
      BatteryVoltage: this.batteryVoltageValue != null ? `${this.batteryVoltageValue} V` : undefined,
      RecordTime: this.recordTime,
      // Always send OFM (even empty) so backend follows request 100% — no template fallback.
      OFM: { Items: this.ofmItems },
    };

    // OEM DTCs: always send (even empty) so backend clears template/base OEM blocks when
    // the user has none — request is authoritative for this UI-editable segment.
    req.OEM_DTCs = this.oemDtcs
      .filter((d) => !d.Code || this.isValidOemCode(d.Code))
      .map((d) => ({ ...d, Code: d.Code ? this.normalizeOemCode(d.Code) : 'No code' }));

    // Merge Tab 2 data (Tab 2 has priority on conflicts)
    req.VinProfile = { ...this.vinProfile, Vin: this.vin || undefined };
    // Monitors / FreezeFrame / DTCs: always send (even empty arrays) so backend clears the
    // corresponding buffer regions when the user removed all items — no template/base fallback.
    req.ECU_Monitors = this.ecuMonitors.filter((m) => m.Name);
    req.TCU_Monitors = this.tcuMonitors.filter((m) => m.Name);
    req.ECU_FreezeFrame = this.ecuFreezeFrame.filter((ff) => ff.Name);
    req.TCU_FreezeFrame = this.tcuFreezeFrame.filter((ff) => ff.Name);
    {
      const valid = this.bufEcuDtcs.filter((d) => d.code && this.isValidEcuCode(d.code));
      req.BufECU_DTCs = valid.map((d): DtcEntry => ({ Code: d.code, Type: d.type }));
    }
    {
      const valid = this.bufTcuDtcs.filter((d) => d.code && this.isValidEcuCode(d.code));
      req.BufTCU_DTCs = valid.map((d): DtcEntry => ({ Code: d.code, Type: d.type }));
    }

    const startedAt = Date.now();
    this.generatingFor.set('payload');
    this.lastGenerated.set('payload');
    this.loading.set(true);
    this.result.set('');
    this.payloadService.generate(req).subscribe({
      next: (res) => this.finishGenerate(startedAt, res.payload),
      error: (err) => this.finishGenerate(startedAt, 'Error: ' + (err.error?.message || err.message)),
    });
  }

  /** Clear the generate loading state after the minimum visible delay, then reveal the result. */
  private finishGenerate(startedAt: number, output: string): void {
    this.runWithMinDelay(startedAt, () => {
      this.result.set(output);
      this.generatingFor.set('');
      this.loading.set(false);
      this.scrollToResult();
    });
  }

  generateBuffers() {
    const req: GenerateVehicleBuffersRequest = {
      BasePayload: this.importedBasePayload() || undefined,
      UsbProductId: this.usbProductId ?? undefined,
      VinProfile: { ...this.vinProfile, Vin: this.vin || undefined },
    };

    // Always send these arrays (even empty) — request is authoritative for UI-editable segments.
    req.ECU_Monitors = this.ecuMonitors.filter((m) => m.Name);
    req.TCU_Monitors = this.tcuMonitors.filter((m) => m.Name);
    req.ECU_FreezeFrame = this.ecuFreezeFrame.filter((ff) => ff.Name);
    req.TCU_FreezeFrame = this.tcuFreezeFrame.filter((ff) => ff.Name);
    {
      const valid = this.bufEcuDtcs.filter((d) => d.code && this.isValidEcuCode(d.code));
      req.ECU_DTCs = valid.map((d): DtcEntry => ({ Code: d.code, Type: d.type }));
    }
    {
      const valid = this.bufTcuDtcs.filter((d) => d.code && this.isValidEcuCode(d.code));
      req.TCU_DTCs = valid.map((d): DtcEntry => ({ Code: d.code, Type: d.type }));
    }
    req.OEM_DTCs = this.oemDtcs
      .filter((d) => !d.Code || this.isValidOemCode(d.Code))
      .map((d) => ({ ...d, Code: d.Code ? this.normalizeOemCode(d.Code) : 'No code' }));

    const startedAt = Date.now();
    this.generatingFor.set('buffers');
    this.lastGenerated.set('buffers');
    this.loading.set(true);
    this.result.set('');
    this.payloadService.generateVehicleBuffers(req).subscribe({
      next: (res) => this.finishGenerate(startedAt, JSON.stringify(res, null, 2)),
      error: (err) => this.finishGenerate(startedAt, 'Error: ' + (err.error?.message || err.message)),
    });
  }

  copyResult() {
    this.writeClipboard(this.result());
    this.copied.set(true);
    setTimeout(() => this.copied.set(false), 2000);
  }

  private writeClipboard(text: string): void {
    if (navigator.clipboard?.writeText) {
      navigator.clipboard.writeText(text);
    } else {
      const ta = document.createElement('textarea');
      ta.value = text;
      ta.style.position = 'fixed';
      ta.style.opacity = '0';
      document.body.appendChild(ta);
      ta.select();
      document.execCommand('copy');
      document.body.removeChild(ta);
    }
  }
}
