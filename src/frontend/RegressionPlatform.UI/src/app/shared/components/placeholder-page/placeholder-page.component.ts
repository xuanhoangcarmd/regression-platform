import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Temporary stand-in page shown while a feature's real UI is not yet built.
 * `title` is bound from the route's `data` via withComponentInputBinding().
 */
@Component({
  selector: 'app-placeholder-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section>
      @if (title()) {
        <h1 class="font-headline-md text-headline-md text-on-surface">{{ title() }}</h1>
      }
      <p class="mt-2 text-body-sm text-secondary">Foundation ready — UI coming soon.</p>
    </section>
  `,
})
export class PlaceholderPageComponent {
  readonly title = input<string>('');
}
