// Dev-server deployment (IIS at https://dev-core-service.innova.com/regression).
// The API lives on a different host, so apiBaseUrl is absolute — cross-origin calls are
// allowed by the backend's CORS policy (Cors:AllowedOrigins includes this frontend origin).
// Wired via the `dev` build configuration in angular.json (fileReplacements + baseHref).
export const environment = {
  production: true,
  // apiBaseUrl: 'https://dev-service6.innova.com/regression-platform-api/api',
  apiBaseUrl: 'http://localhost:5000/api',
  payloadGenApiBaseUrl: 'https://dev-core-service.innova.com/payload-v6-generator/api',
};
