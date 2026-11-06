import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatIconModule } from '@angular/material/icon';

import { ProjectApiService } from '@core/apis';
import { AppIconsConstants, RouteConstants } from '@core/constants';
import { ProjectContextService } from '@core/services';

import { projectIconFor, projectModuleLinks } from '../../config/sidebar-nav.data';
import { ISidebarProject } from '../../models/sidebar-nav.model';
import { SidebarNavItemComponent } from '../sidebar-nav-item/sidebar-nav-item.component';
import { SidebarProjectGroupComponent } from '../sidebar-project-group/sidebar-project-group.component';

/**
 * Main navigation rail. Composes the brand header, the top-level Dashboard link,
 * the expandable Projects section (loaded from GET /api/projects), and the Settings footer.
 */
@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [MatIconModule, SidebarNavItemComponent, SidebarProjectGroupComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './sidebar.component.html',
})
export class SidebarComponent {
  private readonly projectApi = inject(ProjectApiService);
  private readonly projectContext = inject(ProjectContextService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly projects = signal<ISidebarProject[]>([]);

  protected readonly projectsExpanded = signal(true);
  protected readonly openProjectId = signal<string | null>(null);

  protected readonly brandIcon = AppIconsConstants.brand;
  protected readonly homeIcon = AppIconsConstants.home;
  protected readonly dataLibraryIcon = AppIconsConstants.dataLibrary;
  protected readonly projectsIcon = AppIconsConstants.projects;
  protected readonly settingsIcon = AppIconsConstants.settings;
  protected readonly expandLessIcon = AppIconsConstants.expandLess;
  protected readonly expandMoreIcon = AppIconsConstants.expandMore;

  protected readonly dashboardRoute = ['/', RouteConstants.dashboard];
  protected readonly dataLibraryRoute = ['/', RouteConstants.dataLibrary];
  protected readonly settingsRoute = ['/', RouteConstants.settings];

  constructor() {
    this.projectApi
      .getProjects()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((projects) => {
        this.projects.set(
          projects.map((project) => ({
            id: project.code,
            name: project.name,
            icon: projectIconFor(project.code),
            links: projectModuleLinks(project.code),
          })),
        );
        // Expand the project from the current URL, else the first one.
        const active = this.projectContext.code();
        this.openProjectId.set(active || (projects.length > 0 ? projects[0].code : null));
      });
  }

  toggleProjects(): void {
    this.projectsExpanded.update((open) => !open);
  }

  toggleProject(id: string): void {
    this.openProjectId.update((current) => (current === id ? null : id));
  }
}
