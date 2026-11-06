import { AppIconsConstants, ProjectRouteConstants, RouteConstants } from '@core/constants';

import { ISidebarLink } from '../models/sidebar-nav.model';

/**
 * Sidebar navigation config. Projects are loaded from the API (GET /api/projects) by the
 * sidebar component; this file just builds each project's module links and maps an icon.
 *
 * Each module link is project-scoped: /projects/:projectId/<module>.
 */
export const projectModuleLinks = (projectCode: string): ISidebarLink[] => {
  const base = ['/', RouteConstants.projects, projectCode];
  return [
    // Dashboard menu hidden for now.
    // { label: 'Dashboard', icon: AppIconsConstants.dashboard, route: [...base, ProjectRouteConstants.dashboard] },
    { label: 'APIs', icon: AppIconsConstants.apis, route: [...base, ProjectRouteConstants.apis] },
    { label: 'Datasets', icon: AppIconsConstants.datasets, route: [...base, ProjectRouteConstants.datasets] },
    { label: 'Runs', icon: AppIconsConstants.runs, route: [...base, ProjectRouteConstants.runs] },
    { label: 'Compare', icon: AppIconsConstants.compare, route: [...base, ProjectRouteConstants.compare] },
    { label: 'Reports', icon: AppIconsConstants.reports, route: [...base, ProjectRouteConstants.reports] },
    { label: 'Ignore Rules', icon: AppIconsConstants.ignoreRules, route: [...base, ProjectRouteConstants.ignoreRules] },
    {
      label: 'Configurations',
      icon: AppIconsConstants.configurations,
      route: [...base, ProjectRouteConstants.configurations],
    },
  ];
};

/** Icon per known project code, falling back to a generic project node icon. */
const PROJECT_ICONS: Record<string, string> = {
  oreilly: AppIconsConstants.projectOreilly,
  autozone: AppIconsConstants.projectAutozone,
  carid: AppIconsConstants.projectUniversal,
  internal: AppIconsConstants.projectNode,
};

export const projectIconFor = (code: string): string => PROJECT_ICONS[code] ?? AppIconsConstants.projectNode;
