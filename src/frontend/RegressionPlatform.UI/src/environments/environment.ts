// Production environment. The development overrides live in environment.development.ts
// (wired via fileReplacements in angular.json).
export const environment = {
  production: true,
  // Same-origin API gateway in production. Adjust if the API is hosted elsewhere.
  apiBaseUrl: '/api',
  // Deployed Payload V6 Generator API (external service). Used by the Payload Generator page.
  payloadGenApiBaseUrl: 'https://dev-core-service.innova.com/payload-v6-generator/api',
};
