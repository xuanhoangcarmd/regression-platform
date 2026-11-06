/** Distinct YMME options from VehicleProfile data (faceted by current selections; nulls excluded). */
export interface IYmmeOptions {
  years: string[];
  makes: string[];
  models: string[];
  engines: string[];
}

/** A row in the Data Library search results. */
export interface IVehicleProfileListItem {
  id: number;
  vin: string | null;
  year: string | null;
  make: string | null;
  model: string | null;
  engineType: string | null;
  toolMilStatus: number | null;
  payloadCount: number;
  dtcCount: number;
  rawBufferCount: number;
}

export interface IVehicleDtc {
  id: number;
  source: string;
  code: string;
  statusType: string | null;
  system: string | null;
  subSystem: string | null;
  definition: string | null;
}

export interface IPayloadMeta {
  id: number;
  name: string | null;
  byteSize: number | null;
  source: string | null;
  createdDate: string;
}

export interface IRawBufferMeta {
  id: number;
  bufferType: string;
  segmentIndex: number | null;
  byteSize: number | null;
}

/** Full detail of a vehicle profile (snapshot + DTCs + payload/buffer metadata). */
export interface IVehicleProfileDetail {
  id: number;
  sourceId: string | null;
  vin: string | null;
  year: string | null;
  make: string | null;
  model: string | null;
  engineType: string | null;
  trim: string | null;
  transmission: string | null;
  usbProductId: number | null;
  toolMilStatus: number | null;
  primaryDtc: string | null;
  toolLedStatus: number | null;
  sil: string | null;
  absLightStatus: string | null;
  tpmsLightStatus: string | null;
  batteryConditionStatus: string | null;
  batteryVoltage: string | null;
  odometer: number | null;
  mileage: number | null;
  hasRecordTime: boolean;
  hasFreezeFrames: boolean;
  hasMonitors: boolean;
  notes: string | null;
  dtcs: IVehicleDtc[];
  payloads: IPayloadMeta[];
  rawBuffers: IRawBufferMeta[];
}

/** Decompressed payload content. */
export interface IPayloadContent {
  id: number;
  name: string | null;
  base64: string;
  byteSize: number | null;
  source: string | null;
}

/** Decompressed raw buffer content. */
export interface IRawBufferContent {
  id: number;
  bufferType: string;
  segmentIndex: number | null;
  base64: string;
  byteSize: number | null;
}

/** Sortable YMME fields for the table header sort popover. */
export type VehicleProfileSortField = 'year' | 'make' | 'model' | 'engine';

/** How a DTC keyword is matched against the code. */
export type DtcMatchMode = 'contains' | 'equals' | 'startsWith' | 'endsWith';

/** Search filter inputs. */
export interface IVehicleProfileSearch {
  vin?: string;
  year?: string;
  make?: string;
  model?: string;
  engine?: string;
  /** Numeric year lower bound (>=). */
  yearFrom?: number;
  /** Numeric year upper bound (<=). */
  yearTo?: number;
  toolMilStatus?: number;
  toolLedStatus?: number;
  primaryDtc?: string;
  /** Match mode for the Primary DTC keyword: contains (default) | equals | startsWith | endsWith. */
  primaryDtcMode?: DtcMatchMode;
  dtcCode?: string;
  /** Match mode for the OEM DTC keyword: contains (default) | equals | startsWith | endsWith. */
  dtcCodeMode?: DtcMatchMode;
  dtcSource?: string;
  dtcSystem?: string;
  dtcStatusType?: string;
  dtcDefinition?: string;
  /** Preset DTC code-format filter (see DtcFormatOption). */
  dtcFormat?: string;
  /** DTC code-prefix category: P | B | C | U | number | not-pbcu. */
  dtcPrefix?: string;
  hasRawBuffer?: boolean;
  sortBy?: VehicleProfileSortField;
  sortDirection?: 'asc' | 'desc';
  pageNumber: number;
  pageSize: number;
}
