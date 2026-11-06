import { Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRouteSnapshot, NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

/**
 * Tracks the active project code (the `:projectId` route segment, e.g. "oreilly") so data-access
 * services can scope every request with `?projectId=`. Updated on each navigation.
 */
@Injectable({ providedIn: 'root' })
export class ProjectContextService {
  private readonly router = inject(Router);

  /** Current project code from the URL ("" when not inside a project route). */
  readonly code = signal<string>('');

  constructor() {
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.code.set(this.extractCode()));

    this.code.set(this.extractCode());
  }

  private extractCode(): string {
    let route: ActivatedRouteSnapshot | null = this.router.routerState.snapshot.root;
    while (route) {
      const code = route.params['projectId'];
      if (code) {
        return code;
      }
      route = route.firstChild;
    }
    return '';
  }
}
