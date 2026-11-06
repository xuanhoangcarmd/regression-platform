import { ApiHttpMethod, IApiDefinition } from '../models/api-definition.model';

/**
 * TEMPORARY mock dataset until the backend `GET /api/.../apis` endpoint exists.
 * The service paginates/filters/sorts over this in-memory list to mimic the
 * server contract, so swapping in a real ApiDefinitionApiService needs no
 * component changes. Delete this file once the API is wired.
 */
const BASE: IApiDefinition[] = [
  {
    id: 1,
    name: 'GetMostLikelyFixForVehicle',
    method: 'POST',
    endpoint: '/api/v1/fix/match',
    isActive: true,
    requestCount: 2450,
  },
  {
    id: 2,
    name: 'GetDTCLibraryErrorCode',
    method: 'GET',
    endpoint: '/api/v1/dtc/{code}',
    isActive: true,
    requestCount: 1120,
  },
  {
    id: 3,
    name: 'GetPartsByVIN',
    method: 'POST',
    endpoint: '/api/v1/parts/by-vin',
    isActive: true,
    requestCount: 3210,
  },
  {
    id: 4,
    name: 'GetVehicleInfo',
    method: 'GET',
    endpoint: '/api/v1/vehicle/{vin}',
    isActive: true,
    requestCount: 1890,
  },
  { id: 5, name: 'GetLaborTimes', method: 'POST', endpoint: '/api/v1/labor/time', isActive: true, requestCount: 2180 },
];

const METHODS: ApiHttpMethod[] = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE'];

const GENERATED: IApiDefinition[] = Array.from({ length: 121 }, (_, i) => {
  const id = i + 6;
  return {
    id,
    name: `SampleApiOperation${id}`,
    method: METHODS[i % METHODS.length],
    endpoint: `/api/v1/resource/${id}`,
    isActive: id % 7 !== 0,
    requestCount: (id * 137) % 5000,
  };
});

export const MOCK_API_DEFINITIONS: IApiDefinition[] = [...BASE, ...GENERATED];
