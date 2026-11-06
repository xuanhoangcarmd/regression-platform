import { DOCUMENT } from '@angular/common';
import { Injectable, computed, effect, inject, signal } from '@angular/core';

import { StorageConstants } from '@core/constants';

export type ThemeMode = 'light' | 'dark';

/**
 * Controls light/dark theming by toggling `data-theme` on the root element.
 * All colors come from the CSS custom properties in src/styles/_tokens.scss,
 * so flipping this attribute re-themes the entire UI (Tailwind + Material).
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);

  /** Current theme mode. */
  readonly mode = signal<ThemeMode>(this.resolveInitialMode());

  /** Convenience flag for templates. */
  readonly isDark = computed(() => this.mode() === 'dark');

  constructor() {
    // Apply on init and on every change.
    effect(() => this.applyMode(this.mode()));
  }

  toggle(): void {
    this.mode.update((current) => (current === 'dark' ? 'light' : 'dark'));
  }

  setMode(mode: ThemeMode): void {
    this.mode.set(mode);
  }

  private applyMode(mode: ThemeMode): void {
    const root = this.document.documentElement;
    if (mode === 'dark') {
      root.setAttribute('data-theme', 'dark');
    } else {
      root.removeAttribute('data-theme');
    }

    try {
      this.document.defaultView?.localStorage.setItem(StorageConstants.theme, mode);
    } catch {
      // localStorage may be unavailable (private mode / SSR) — ignore.
    }
  }

  private resolveInitialMode(): ThemeMode {
    try {
      const saved = this.document.defaultView?.localStorage.getItem(StorageConstants.theme);
      if (saved === 'light' || saved === 'dark') {
        return saved;
      }
    } catch {
      // ignore and fall back to system preference
    }

    const prefersDark = this.document.defaultView?.matchMedia('(prefers-color-scheme: dark)').matches;
    return prefersDark ? 'dark' : 'light';
  }
}
