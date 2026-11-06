import { NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

import { AppIconsConstants } from '@core/constants';

import { ISidebarProject } from '../../models/sidebar-nav.model';
import { SidebarNavItemComponent } from '../sidebar-nav-item/sidebar-nav-item.component';

/**
 * Expandable project node: a header button that toggles its nested module links.
 * Expansion state is controlled by the parent (accordion behavior).
 */
@Component({
  selector: 'app-sidebar-project-group',
  standalone: true,
  imports: [NgClass, MatIconModule, SidebarNavItemComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './sidebar-project-group.component.html',
})
export class SidebarProjectGroupComponent {
  readonly project = input.required<ISidebarProject>();
  readonly expanded = input.required<boolean>();
  readonly toggled = output<void>();

  protected readonly unfoldMoreIcon = AppIconsConstants.unfoldMore;
  protected readonly expandMoreIcon = AppIconsConstants.expandMore;
}
