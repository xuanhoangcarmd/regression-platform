import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { MAT_ICON_DEFAULT_OPTIONS } from '@angular/material/icon';
import { PreloadAllModules, provideRouter, withComponentInputBinding, withPreloading } from '@angular/router';

import { authInterceptor, correlationIdInterceptor, errorInterceptor, loadingInterceptor } from '@core/interceptors';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    // Bind route params/query/data to component inputs (routing.md).
    // Preload all lazy route chunks in the background after the first render, so navigating
    // between menus is instant (no janky per-route chunk fetch on the first click).
    provideRouter(routes, withComponentInputBinding(), withPreloading(PreloadAllModules)),
    provideAnimationsAsync(),
    // Functional interceptors only. Order: loading wraps the chain; error is
    // innermost so it sees the raw response first (http.md).
    provideHttpClient(
      withInterceptors([loadingInterceptor, correlationIdInterceptor, authInterceptor, errorInterceptor]),
    ),
    // Default <mat-icon> to the thin outline set (design-tokens.md §3).
    { provide: MAT_ICON_DEFAULT_OPTIONS, useValue: { fontSet: 'material-symbols-outlined' } },
  ],
};
