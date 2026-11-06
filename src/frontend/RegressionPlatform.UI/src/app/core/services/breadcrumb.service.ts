import { Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRouteSnapshot, NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

/** One segment of the header breadcrumb trail. */
export interface IBreadcrumb {
  label: string;
  /** Router link for this crumb; null for the current (last) page. */
  url: string | null;
}

/** A route's breadcrumb label — a static string or a function of the matched route (for dynamic ids). */
export type BreadcrumbLabel = string | ((route: ActivatedRouteSnapshot) => string);

/**
 * Builds the header breadcrumb trail from the activated route tree. A route opts in via
 * `data: { breadcrumb: 'Label' | (route) => 'Label' }` (falls back to `data.title`). Updated
 * on every navigation; the last crumb is the current page (rendered as plain text, not a link).
 */
@Injectable({ providedIn: 'root' })
export class BreadcrumbService {
  private readonly router = inject(Router);

  readonly breadcrumbs = signal<IBreadcrumb[]>([]);

  constructor() {
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.breadcrumbs.set(this.build()));

    this.breadcrumbs.set(this.build());
  }

  private build(): IBreadcrumb[] {
    const crumbs: IBreadcrumb[] = [];
    let url = '';
    let route: ActivatedRouteSnapshot | null = this.router.routerState.snapshot.root;

    while (route) {
      const segment = route.url.map((part) => part.path).join('/');
      if (segment) {
        url += '/' + segment;
      }

      const data = route.routeConfig?.data ?? {};
      const raw = (data['breadcrumb'] ?? data['title']) as BreadcrumbLabel | undefined;
      if (raw) {
        const label = typeof raw === 'function' ? raw(route) : raw;
        if (label) {
          crumbs.push({ label, url });
        }
      }

      route = route.firstChild;
    }

    // The last crumb is the current page — not a link.
    if (crumbs.length > 0) {
      crumbs[crumbs.length - 1] = { ...crumbs[crumbs.length - 1], url: null };
    }
    return crumbs;
  }
}
