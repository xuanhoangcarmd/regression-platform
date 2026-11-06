/**
 * Tailwind theme — "Technical Precision System" (design-tokens.md / DESIGN.md).
 * Every color points at a CSS custom property defined in src/styles/_tokens.scss
 * (the SINGLE source of truth), so editing a token there updates Tailwind and
 * dark mode automatically. This file holds NO hardcoded color values.
 *
 * Preflight is disabled so Tailwind's reset does not override Angular Material;
 * minimal base resets live in styles.scss.
 */
/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  darkMode: ['selector', ':root[data-theme="dark"]'],
  corePlugins: {
    preflight: false,
  },
  theme: {
    extend: {
      colors: {
        primary: 'var(--rp-primary)',
        'on-primary': 'var(--rp-on-primary)',
        'primary-container': 'var(--rp-primary-container)',
        'on-primary-container': 'var(--rp-on-primary-container)',
        'inverse-primary': 'var(--rp-inverse-primary)',

        secondary: 'var(--rp-secondary)',
        'on-secondary': 'var(--rp-on-secondary)',
        'secondary-container': 'var(--rp-secondary-container)',
        'on-secondary-container': 'var(--rp-on-secondary-container)',

        tertiary: 'var(--rp-tertiary)',
        'on-tertiary': 'var(--rp-on-tertiary)',
        'tertiary-container': 'var(--rp-tertiary-container)',
        'on-tertiary-container': 'var(--rp-on-tertiary-container)',

        error: 'var(--rp-error)',
        'on-error': 'var(--rp-on-error)',
        'error-container': 'var(--rp-error-container)',
        'on-error-container': 'var(--rp-on-error-container)',

        background: 'var(--rp-background)',
        'on-background': 'var(--rp-on-background)',
        surface: 'var(--rp-surface)',
        'surface-dim': 'var(--rp-surface-dim)',
        'surface-bright': 'var(--rp-surface-bright)',
        'surface-container-lowest': 'var(--rp-surface-container-lowest)',
        'surface-container-low': 'var(--rp-surface-container-low)',
        'surface-container': 'var(--rp-surface-container)',
        'surface-container-high': 'var(--rp-surface-container-high)',
        'surface-container-highest': 'var(--rp-surface-container-highest)',
        'surface-variant': 'var(--rp-surface-variant)',
        'on-surface': 'var(--rp-on-surface)',
        'on-surface-variant': 'var(--rp-on-surface-variant)',

        outline: 'var(--rp-outline)',
        'outline-variant': 'var(--rp-outline-variant)',
        'surface-tint': 'var(--rp-surface-tint)',
        'inverse-surface': 'var(--rp-inverse-surface)',
        'inverse-on-surface': 'var(--rp-inverse-on-surface)',

        // Sidebar rail (always-dark tokens)
        sidebar: {
          DEFAULT: 'var(--rp-sidebar)',
          on: 'var(--rp-sidebar-on)',
          muted: 'var(--rp-sidebar-muted)',
          hover: 'var(--rp-sidebar-hover)',
          active: 'var(--rp-sidebar-active)',
          line: 'var(--rp-sidebar-line)',
        },
      },
      fontFamily: {
        'display-lg': ['Hanken Grotesk', 'Inter', 'system-ui', 'sans-serif'],
        'headline-md': ['Hanken Grotesk', 'Inter', 'system-ui', 'sans-serif'],
        'title-sm': ['Hanken Grotesk', 'Inter', 'system-ui', 'sans-serif'],
        'body-md': ['Inter', 'system-ui', 'sans-serif'],
        'body-sm': ['Inter', 'system-ui', 'sans-serif'],
        'label-xs': ['Inter', 'system-ui', 'sans-serif'],
        'code-sm': ['JetBrains Mono', 'ui-monospace', 'monospace'],
      },
      fontSize: {
        'display-lg': ['30px', { lineHeight: '38px', fontWeight: '700' }],
        'headline-md': ['20px', { lineHeight: '28px', fontWeight: '600' }],
        'title-sm': ['16px', { lineHeight: '24px', fontWeight: '600' }],
        'body-md': ['14px', { lineHeight: '20px', fontWeight: '400' }],
        'body-sm': ['12px', { lineHeight: '18px', fontWeight: '400' }],
        'label-xs': ['11px', { lineHeight: '14px', letterSpacing: '0.05em', fontWeight: '600' }],
        'code-sm': ['12px', { lineHeight: '16px', fontWeight: '400' }],
      },
      spacing: {
        base: '4px',
        gutter: '16px',
        'container-padding': '24px',
        'row-height-md': '48px',
        'row-height-sm': '36px',
      },
      borderRadius: {
        sm: '0.125rem',
        DEFAULT: '0.25rem',
        md: '0.375rem',
        lg: '0.5rem',
        xl: '0.75rem',
        full: '9999px',
      },
    },
  },
  plugins: [],
};
