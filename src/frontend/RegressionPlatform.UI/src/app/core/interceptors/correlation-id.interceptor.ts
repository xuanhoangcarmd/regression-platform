import { HttpInterceptorFn } from '@angular/common/http';

/**
 * Stamps every outgoing request with a unique correlation id so a request can be
 * traced across the frontend and backend logs. See docs/rules/angular/http.md.
 */
export const correlationIdInterceptor: HttpInterceptorFn = (req, next) => {
  const correlationId = crypto.randomUUID();
  return next(req.clone({ setHeaders: { 'X-Correlation-Id': correlationId } }));
};
