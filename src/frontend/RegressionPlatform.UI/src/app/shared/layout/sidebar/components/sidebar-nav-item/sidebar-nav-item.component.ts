import { NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink, RouterLinkActive } from '@angular/router';

/**
 * Presentational sidebar row: icon + label as a router link. Highlights itself
 * via routerLinkActive. Reused for top-level links and nested module links.
 */
@Component({
  selector: 'app-sidebar-nav-item',
  standalone: true,
  imports: [NgClass, MatIconModule, RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <a
      [routerLink]="route()"
      routerLinkActive
      #rla="routerLinkActive"
      [routerLinkActiveOptions]="{ exact: exact() }"
      class="flex items-center gap-3 rounded-lg px-4 py-2 text-body-sm transition-colors"
      [ngClass]="
        rla.isActive
          ? 'bg-sidebar-active text-sidebar-on font-medium'
          : 'text-sidebar-muted hover:bg-sidebar-hover hover:text-sidebar-on'
      "
    >
      <mat-icon class="text-[20px]">{{ icon() }}</mat-icon>
      <span>{{ label() }}</span>
    </a>
  `,
})
export class SidebarNavItemComponent {
  readonly icon = input.required<string>();
  readonly label = input.required<string>();
  readonly route = input.required<string[]>();
  readonly exact = input<boolean>(false);
}
