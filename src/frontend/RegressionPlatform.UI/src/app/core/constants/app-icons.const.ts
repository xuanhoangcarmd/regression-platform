/**
 * Central icon registry — Material Symbols Outlined names (design-tokens.md §3).
 * Reference icons by semantic key (e.g. AppIconsConstants.runs) instead of raw
 * strings, so swapping an icon is a one-line change here and updates everywhere.
 */
export const AppIconsConstants = {
  // ----- Branding -----
  brand: 'shield',

  // ----- Top-level navigation -----
  home: 'home',
  projects: 'folder',
  dataLibrary: 'database',
  payloadGenerator: 'bolt',
  settings: 'settings',

  // ----- Project node icons -----
  projectOreilly: 'target',
  projectAutozone: 'directions_car',
  projectUniversal: 'public',
  projectNode: 'desktop_windows',

  // ----- Project module navigation (nested) -----
  dashboard: 'grid_view',
  apis: 'code',
  requestPool: 'inbox',
  datasets: 'dataset',
  runs: 'history',
  compare: 'compare',
  reports: 'description',
  ignoreRules: 'rule',
  configurations: 'assignment',

  // ----- Top navigation bar -----
  menu: 'menu',
  notifications: 'notifications',
  help: 'help_outline',
  chevronRight: 'chevron_right',

  // ----- Expand / collapse / sort -----
  arrowForward: 'arrow_forward',
  expandMore: 'expand_more',
  expandLess: 'expand_less',
  unfoldMore: 'unfold_more',
  sortNone: 'unfold_more',
  sortAsc: 'arrow_upward',
  sortDesc: 'arrow_downward',
  chevronLeft: 'chevron_left',

  // ----- Common actions -----
  add: 'add',
  edit: 'edit',
  delete: 'delete',
  view: 'visibility',
  start: 'play_arrow',
  stop: 'stop',
  search: 'search',
  filter: 'filter_list',
  refresh: 'refresh',
  download: 'download',
  upload: 'upload_file',
  more: 'more_vert',
  close: 'close',
  copy: 'content_copy',
  pin: 'star',
  ai: 'auto_awesome',
  pdf: 'picture_as_pdf',
  email: 'mail',
  share: 'share',

  // ----- Run metadata -----
  environment: 'dns',
  runType: 'category',
  schedule: 'schedule',
  duration: 'timer',

  // ----- Status -----
  success: 'check_circle',
  error: 'cancel',
  warning: 'warning',
  info: 'info',

  // ----- Theme toggle -----
  themeLight: 'light_mode',
  themeDark: 'dark_mode',
} as const;

export type AppIcon = keyof typeof AppIconsConstants;
