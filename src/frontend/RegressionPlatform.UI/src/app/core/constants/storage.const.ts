/**
 * Keys for browser storage (localStorage / sessionStorage).
 * Centralized to avoid duplicated/clashing string keys.
 */
export const StorageConstants = {
  theme: 'rp-theme',
  authToken: 'rp-auth-token',
  /** Live Data Result export: context-field rules (e.g. show the DTC next to a changed Definition). */
  liveDataContextRules: 'rp-live-data-context-rules',
} as const;
