import { Injectable, computed, signal } from '@angular/core';

/**
 * Tracks in-flight HTTP requests via a reference count so overlapping requests
 * don't prematurely hide a global loading indicator. Driven by loadingInterceptor.
 */
@Injectable({ providedIn: 'root' })
export class LoadingService {
  private readonly activeRequests = signal(0);

  /** True while one or more requests are in flight. */
  readonly isLoading = computed(() => this.activeRequests() > 0);

  start(): void {
    this.activeRequests.update((count) => count + 1);
  }

  stop(): void {
    this.activeRequests.update((count) => Math.max(0, count - 1));
  }
}
