import { HttpInterceptorFn } from '@angular/common/http';

import { StorageConstants } from '@core/constants';

/**
 * Attaches the JWT bearer token (if present) to outgoing requests.
 * NOTE: Stub — reads the token from localStorage directly. Replace the lookup
 * with a TokenService once authentication is implemented. See http.md.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  let token: string | null = null;
  try {
    token = localStorage.getItem(StorageConstants.authToken);
  } catch {
    // localStorage may be unavailable — proceed without a token.
  }

  if (!token) {
    return next(req);
  }

  return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};
