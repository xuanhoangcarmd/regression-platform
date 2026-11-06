import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, shareReplay } from 'rxjs';

import { environment } from '../../../../environments/environment';

export interface DtcEntry {
  Code: string;
  Type: string;
}

export interface OemDtcInput {
  MakeEnum: number;
  System: number;
  SubSystem: number;
  Code: string;
  Group: number;
  Types: string[];
  // Preserved from import for roundtrip accuracy (not exposed in UI)
  DtcDisplayType?: number;
  FormatDtc?: number;
  Protocol?: number;
  BlockIndex?: number;
}

export interface MonitorInput {
  Name: string;
  Key: number;
}

export interface FreezeFrameItemInput {
  Name: string;
  Unit: string;
  Values: string[];
  Pid?: number;
}

export interface FreezeFrameItemDef {
  name: string;
  description: string;
  unit: string;
  pid: number;
  dataSize: number;
  bytePos: number;
  byteSize: number;
  a: number | null;
  b: number | null;
  sign: string | null;
  tableId: string | null;
}

export interface RecordTimeInput {
  OBD2?: number | null;
  ABS?: number | null;
  TCM?: number | null;
  ODO?: number | null;
  ECM_PCM?: number | null;
  SIL?: number | null;
  ABS_LIGHT?: number | null;
  OFM_ITEMS?: number | null;
  TPMS?: number | null;
  TPMS_LIGHT?: number | null;
}

export interface OFMItemInput {
  ItemId: number;
  ItemName: string;
  Value: string;
  Unit: string;
  ItemDescriptionEnum: number;
}

export interface OFMInput {
  Items: OFMItemInput[];
}

export interface GenerateRawPayloadRequest {
  Vin?: string;
  UsbProductId?: number;
  OBD2_MIL?: boolean;
  FF_DTC?: string;
  ECU_DTCs?: DtcEntry[];
  OEM_DTCs?: OemDtcInput[];
  LedStatus?: string;
  SIL?: string;
  ABSLightStatus?: string;
  TPMSLightStatus?: string;
  // Tab 2 buffer overrides (merged into raw payload)
  VinProfile?: VinProfileInput;
  ECU_Monitors?: MonitorInput[];
  TCU_Monitors?: MonitorInput[];
  ECU_FreezeFrame?: FreezeFrameItemInput[];
  TCU_FreezeFrame?: FreezeFrameItemInput[];
  BufECU_DTCs?: DtcEntry[];
  BufTCU_DTCs?: DtcEntry[];
  BasePayload?: string;
  // Non-UI payload segments
  BatteryConditionStatus?: string;
  OdometerValue?: number;
  OdometerUnit?: string;
  BatteryVoltage?: string;
  RecordTime?: RecordTimeInput;
  OFM?: OFMInput;
}

export interface GenerateRawPayloadResponse {
  payload: string;
}

export interface VinProfileInput {
  Vin?: string;
  Manufacturer: number;
  Year: number;
  Make: number;
  Model: number;
  Trim: number;
  BodyCode: number;
  Engine: number;
  Transmission: number;
  MakeGroup: number;
  VehID: number;
}

export interface GenerateVehicleBuffersRequest {
  BasePayload?: string;
  UsbProductId?: number;
  VinProfile?: VinProfileInput;
  ECU_Monitors?: MonitorInput[];
  TCU_Monitors?: MonitorInput[];
  ECU_FreezeFrame?: FreezeFrameItemInput[];
  TCU_FreezeFrame?: FreezeFrameItemInput[];
  ECU_DTCs?: DtcEntry[];
  TCU_DTCs?: DtcEntry[];
  OEM_DTCs?: OemDtcInput[];
}

export interface VehicleBuffersResponse {
  vin: string;
  usbProductId: number;
  vinProfileRaw: string | null;
  monitorStatusEcmRaw: string | null;
  monitorStatusTcmRaw: string | null;
  freezeFrameEcmRaw: string | null;
  freezeFrameTcmRaw: string | null;
  dtcsEcmRaw: string | null;
  dtcsTcmRaw: string | null;
  vehicleInfoEcmRaw: string | null;
  vehicleInfoTcmRaw: string | null;
  oemModuleRaw: string[];
  oemModuleRawPayload: string | null;
}

/**
 * Calls the deployed Payload V6 Generator API (base URL from environment.payloadGenApiBaseUrl —
 * proxied in dev via /payload-gen-api). Generates a full raw payload or per-segment vehicle
 * buffers, parses an imported payload back into form values, and combines OEM module blocks.
 */
@Injectable({ providedIn: 'root' })
export class PayloadGeneratorService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.payloadGenApiBaseUrl;

  private ffCache$?: Observable<FreezeFrameItemDef[]>;

  getFreezeFrameItems(): Observable<FreezeFrameItemDef[]> {
    if (!this.ffCache$) {
      this.ffCache$ = this.http
        .get<FreezeFrameItemDef[]>(`${this.baseUrl}/freeze-frame-items`)
        .pipe(shareReplay(1));
    }
    return this.ffCache$;
  }

  generate(request: GenerateRawPayloadRequest): Observable<GenerateRawPayloadResponse> {
    return this.http.post<GenerateRawPayloadResponse>(`${this.baseUrl}/generate-raw-payload`, request);
  }

  generateVehicleBuffers(request: GenerateVehicleBuffersRequest): Observable<VehicleBuffersResponse> {
    return this.http.post<VehicleBuffersResponse>(`${this.baseUrl}/generate-vehicle-buffers`, request);
  }

  combineOemModule(
    oemModuleRaw: string[],
    vinProfileRaw?: string,
  ): Observable<{ oemModuleRawPayload: string | null }> {
    return this.http.post<{ oemModuleRawPayload: string | null }>(`${this.baseUrl}/combine-oem-module`, {
      OemModuleRaw: oemModuleRaw,
      VinProfileRaw: vinProfileRaw,
    });
  }

  parsePayload(base64Payload: string): Observable<GenerateRawPayloadRequest> {
    return this.http.post<GenerateRawPayloadRequest>(`${this.baseUrl}/parse-payload`, {
      payload: base64Payload,
    });
  }
}
