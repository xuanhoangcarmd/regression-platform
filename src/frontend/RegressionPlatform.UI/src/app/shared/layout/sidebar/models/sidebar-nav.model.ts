/** A single navigable row in the sidebar. */
export interface ISidebarLink {
  label: string;
  /** Material Symbols icon name (from AppIconsConstants). */
  icon: string;
  /** routerLink commands. */
  route: string[];
}

/** A project node with its expandable list of module links. */
export interface ISidebarProject {
  id: string;
  name: string;
  icon: string;
  links: ISidebarLink[];
}
