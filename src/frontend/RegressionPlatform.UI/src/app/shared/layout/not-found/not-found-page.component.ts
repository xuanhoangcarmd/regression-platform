import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

import { RouteConstants } from '@core/constants';

/** Fallback page for unmatched routes (the `**` wildcard). */
@Component({
  selector: 'app-not-found-page',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="flex min-h-screen flex-col items-center justify-center gap-3 bg-background">
      <p class="font-display-lg text-display-lg text-primary">404</p>
      <p class="text-title-sm text-secondary">Page not found</p>
      <a [routerLink]="['/', dashboardPath]" class="text-body-md text-primary hover:underline"> Back to dashboard </a>
    </section>
  `,
})
export class NotFoundPageComponent {
  protected readonly dashboardPath = RouteConstants.dashboard;
}
