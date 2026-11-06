/**
 * Keys for browser storage (localStorage / sessionStorage).
 * Centralized to avoid duplicated/clashing string keys.
 */
export const StorageConstants = {
  theme: 'rp-theme',
  authToken: 'rp-auth-token',
} as const;
