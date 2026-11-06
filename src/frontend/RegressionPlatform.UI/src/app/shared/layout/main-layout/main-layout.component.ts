import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { NavigationCancel, NavigationEnd, NavigationError, NavigationStart, Router, RouterOutlet } from '@angular/router';

import { SidebarComponent } from '../sidebar/components/sidebar/sidebar.component';
import { TopbarComponent } from '../topbar/topbar.component';

/** App shell: fixed sidebar rail + (top navigation bar over the routed content). */
@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [SidebarComponent, TopbarComponent, RouterOutlet, MatProgressBarModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex h-screen overflow-hidden">
      <app-sidebar />
      <div class="flex flex-1 flex-col overflow-hidden">
        <app-topbar />
        <!-- Thin top bar while a route (and its lazy chunk) is loading, so navigation isn't janky. -->
        @if (navigating()) {
          <mat-progress-bar mode="indeterminate" class="!h-0.5" />
        }
        <main class="flex-1 overflow-auto bg-background p-container-padding">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
})
export class MainLayoutComponent {
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  /** True while the router is navigating (drives the top progress bar). */
  protected readonly navigating = signal(false);

  constructor() {
    this.router.events.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((event) => {
      if (event instanceof NavigationStart) {
        this.navigating.set(true);
      } else if (
        event instanceof NavigationEnd ||
        event instanceof NavigationCancel ||
        event instanceof NavigationError
      ) {
        this.navigating.set(false);
      }
    });
  }
}
