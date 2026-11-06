import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

import { ThemeMode, ThemeService } from '@core/services';

/** Settings page. Appearance section lets the user pick the light/dark theme (persisted per machine). */
@Component({
  selector: 'app-settings-page',
  standalone: true,
  imports: [MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './settings-page.component.html',
})
export class SettingsPageComponent {
  protected readonly theme = inject(ThemeService);

  /** Selectable theme options rendered as a segmented control. */
  protected readonly themeOptions: { mode: ThemeMode; label: string; icon: string; hint: string }[] = [
    { mode: 'light', label: 'Light', icon: 'light_mode', hint: 'Bright background' },
    { mode: 'dark', label: 'Dark', icon: 'dark_mode', hint: 'Dim, low-light background' },
  ];

  protected selectTheme(mode: ThemeMode): void {
    this.theme.setMode(mode);
  }
}
