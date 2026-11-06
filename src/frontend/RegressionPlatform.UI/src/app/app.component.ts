import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { ThemeService } from '@core/services';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent {
  // Instantiated at startup so the saved/system theme is applied immediately.
  private readonly themeService = inject(ThemeService);

  title = 'regression-platform-ui';
}
