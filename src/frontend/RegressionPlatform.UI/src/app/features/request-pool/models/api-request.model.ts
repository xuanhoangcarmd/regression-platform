/** A row in the Request Pool list (joined to its API for name + method). */
export interface IApiRequestItem {
  id: number;
  apiId: number;
  apiName: string;
  title: string;
  description: string | null;
  method: string;
  isActive: boolean;
}

export type RequestStatusFilter = 'all' | 'active' | 'inactive';

/** Full request detail (for the edit form prefill). */
export interface IApiRequestDetail {
  id: number;
  apiId: number;
  title: string;
  description?: string | null;
  headersJson?: string | null;
  queryStringJson?: string | null;
  requestBody?: string | null;
  version: number;
  isActive: boolean;
}

/** Create/update payload. */
export interface IApiRequestWrite {
  apiId: number;
  title: string;
  description?: string | null;
  headersJson?: string | null;
  queryStringJson?: string | null;
  requestBody?: string | null;
}

/** Result of an Excel bulk-import (rows found, requests created, rows skipped). */
export interface IRequestImportResult {
  total: number;
  imported: number;
  skipped: number;
}
