export type ApiHttpMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';

export type ApiStatusFilter = 'all' | 'active' | 'inactive';

/**
 * An API endpoint definition. Mirrors the `Api` table (docs/database/Tables.md):
 * Id, Name, Method, Endpoint, Description, IsActive.
 * `requestCount` is a derived aggregate (count of ApiRequest rows for this Api)
 * supplied by the API response — not a column on `Api`.
 */
export interface IApiDefinition {
  id: number;
  name: string;
  method: ApiHttpMethod;
  endpoint: string;
  description?: string;
  isActive: boolean;
  requestCount: number;
}

/** Fields supplied when creating an API (id & requestCount come from the server). */
export type ApiDefinitionCreate = Pick<IApiDefinition, 'name' | 'method' | 'endpoint' | 'isActive'>;

/** Result of importing API definitions from a Swagger URL. */
export interface IApiImportResult {
  total: number;
  imported: number;
  skipped: number;
}
