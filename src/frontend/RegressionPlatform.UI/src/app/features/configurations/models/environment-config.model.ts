/** The environments a project can be configured for. */
export type EnvironmentKey = 'DEV' | 'STG' | 'PROD';

// ---------------------------------------------------------------------------
// UI working state (what the form/store hold).
// ---------------------------------------------------------------------------

/** A header row's working state. `editing` = plaintext/visible; otherwise a stored secret is masked. */
export interface IHeaderState {
  key: string;
  value: string;
  secret: boolean;
  /** A stored value exists on the server (drives masking for secrets). */
  hasValue: boolean;
  editing: boolean;
}

export interface IEnvironmentState {
  baseUrl: string;
  headers: IHeaderState[];
}

export type EnvironmentStateMap = Record<EnvironmentKey, IEnvironmentState>;

// ---------------------------------------------------------------------------
// Backend contracts (GET /api/projects/{code}/configurations).
// ---------------------------------------------------------------------------

/** A header as returned by the API. `value` is null for secret headers (never returned). */
export interface IConfigurationHeaderResponse {
  key: string;
  value: string | null;
  isSecret: boolean;
  hasValue: boolean;
}

export interface IEnvironmentConfigurationResponse {
  environment: EnvironmentKey;
  baseUrl: string | null;
  headers: IConfigurationHeaderResponse[];
}

// ---------------------------------------------------------------------------
// Save payload (PUT /api/projects/{code}/configurations/{environment}).
// ---------------------------------------------------------------------------

/** `valueProvided=false` keeps a stored secret unchanged (value ignored). */
export interface ISaveConfigurationHeader {
  key: string;
  value: string | null;
  isSecret: boolean;
  valueProvided: boolean;
}

export interface ISaveEnvironmentConfiguration {
  baseUrl: string | null;
  headers: ISaveConfigurationHeader[];
}
