import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

import { IProblemDetails } from '@core/models';

/**
 * Normalizes HTTP errors into a typed IProblemDetails and rethrows it, so feature
 * services receive a predictable shape. User-facing notifications are handled by
 * the feature/notification layer, not here. See docs/rules/angular/http.md.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const problem = toProblemDetails(error);
      // TODO: route to NotificationService once the shared layer is implemented.
      console.error('[HTTP error]', problem);
      return throwError(() => problem);
    }),
  );

function toProblemDetails(error: HttpErrorResponse): IProblemDetails {
  // Backend ProblemDetails responses arrive as the parsed error body.
  if (error.error && typeof error.error === 'object' && 'status' in error.error) {
    return error.error as IProblemDetails;
  }

  // Network failure or non-ProblemDetails error — synthesize a minimal one.
  return {
    title: error.status === 0 ? 'Network error' : error.statusText || 'Unexpected error',
    status: error.status,
    detail: error.message,
  };
}
