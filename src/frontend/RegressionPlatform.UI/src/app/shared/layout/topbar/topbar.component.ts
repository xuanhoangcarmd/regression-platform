import { ChangeDetectionStrategy, Component, inject, output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink, RouterLinkActive } from '@angular/router';

import { AppIconsConstants, RouteConstants } from '@core/constants';
import { BreadcrumbService } from '@core/services';

/**
 * Top navigation bar: website breadcrumb (from the route tree), top-level tabs,
 * action icons, and avatar. `menuToggle` fires when the mobile menu button is pressed.
 */
@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [MatIconModule, RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './topbar.component.html',
})
export class TopbarComponent {
  /** Breadcrumb trail for the current route (e.g. Runs › RUN-5). */
  protected readonly breadcrumbs = inject(BreadcrumbService).breadcrumbs;
  readonly menuToggle = output<void>();

  protected readonly menuIcon = AppIconsConstants.menu;
  protected readonly chevronRightIcon = AppIconsConstants.chevronRight;
  protected readonly notificationsIcon = AppIconsConstants.notifications;
  protected readonly helpIcon = AppIconsConstants.help;

  protected readonly dashboardRoute = ['/', RouteConstants.dashboard];

  protected readonly activeTabClass = 'text-primary font-bold border-primary';
  protected readonly inactiveTabClass = 'text-secondary hover:text-primary border-transparent';
}
