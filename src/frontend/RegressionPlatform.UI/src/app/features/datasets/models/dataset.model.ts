/** A row in the Dataset List (with request + distinct-API counts). */
export interface IDatasetItem {
  id: number;
  name: string;
  description?: string | null;
  isActive: boolean;
  requestCount: number;
  apiCount: number;
}

export type DatasetStatusFilter = 'all' | 'active' | 'inactive';

/** A single dataset (detail header / edit prefill). */
export interface IDataset {
  id: number;
  name: string;
  description?: string | null;
  isActive: boolean;
}

/** Create payload. */
export interface IDatasetCreate {
  name: string;
  description?: string | null;
}

/** Update payload (rename / description / active state). */
export interface IDatasetUpdate {
  name: string;
  description?: string | null;
  isActive: boolean;
}

/** An API that has requests in a dataset (for the run-create API picker). */
export interface IDatasetApi {
  id: number;
  name: string;
  method: string;
  endpoint: string;
  requestCount: number;
}
