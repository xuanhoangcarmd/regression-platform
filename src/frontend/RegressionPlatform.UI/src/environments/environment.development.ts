// Development environment. `/api` is proxied to the backend by proxy.conf.json
// (avoids CORS during local dev). Change the proxy target, not this value.
export const environment = {
  production: false,
  apiBaseUrl: '/api',
  // Proxied by proxy.conf.json (/payload-gen-api → dev-core-service) to avoid CORS in dev.
  payloadGenApiBaseUrl: '/payload-gen-api',
};
