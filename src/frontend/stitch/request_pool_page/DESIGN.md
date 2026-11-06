---
name: Technical Precision System
colors:
  surface: '#faf8ff'
  surface-dim: '#d9d9e5'
  surface-bright: '#faf8ff'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f3f2fe'
  surface-container: '#ededf9'
  surface-container-high: '#e8e7f3'
  surface-container-highest: '#e2e1ed'
  on-surface: '#1a1b23'
  on-surface-variant: '#434655'
  inverse-surface: '#2e3039'
  inverse-on-surface: '#f0f0fb'
  outline: '#747686'
  outline-variant: '#c4c5d7'
  surface-tint: '#2151da'
  primary: '#0037b0'
  on-primary: '#ffffff'
  primary-container: '#1d4ed8'
  on-primary-container: '#cad3ff'
  inverse-primary: '#b7c4ff'
  secondary: '#505f76'
  on-secondary: '#ffffff'
  secondary-container: '#d0e1fb'
  on-secondary-container: '#54647a'
  tertiary: '#7f2500'
  on-tertiary: '#ffffff'
  tertiary-container: '#a73400'
  on-tertiary-container: '#ffc9b7'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#dce1ff'
  primary-fixed-dim: '#b7c4ff'
  on-primary-fixed: '#001551'
  on-primary-fixed-variant: '#0039b5'
  secondary-fixed: '#d3e4fe'
  secondary-fixed-dim: '#b7c8e1'
  on-secondary-fixed: '#0b1c30'
  on-secondary-fixed-variant: '#38485d'
  tertiary-fixed: '#ffdbcf'
  tertiary-fixed-dim: '#ffb59c'
  on-tertiary-fixed: '#390c00'
  on-tertiary-fixed-variant: '#832700'
  background: '#faf8ff'
  on-background: '#1a1b23'
  surface-variant: '#e2e1ed'
typography:
  display-lg:
    fontFamily: Hanken Grotesk
    fontSize: 30px
    fontWeight: '700'
    lineHeight: 38px
  headline-md:
    fontFamily: Hanken Grotesk
    fontSize: 20px
    fontWeight: '600'
    lineHeight: 28px
  title-sm:
    fontFamily: Hanken Grotesk
    fontSize: 16px
    fontWeight: '600'
    lineHeight: 24px
  body-md:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  body-sm:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '400'
    lineHeight: 18px
  code-sm:
    fontFamily: JetBrains Mono
    fontSize: 12px
    fontWeight: '400'
    lineHeight: 16px
  label-xs:
    fontFamily: Inter
    fontSize: 11px
    fontWeight: '600'
    lineHeight: 14px
    letterSpacing: 0.05em
rounded:
  sm: 0.125rem
  DEFAULT: 0.25rem
  md: 0.375rem
  lg: 0.5rem
  xl: 0.75rem
  full: 9999px
spacing:
  base: 4px
  container-padding: 24px
  gutter: 16px
  row-height-md: 48px
  row-height-sm: 36px
---

## Brand & Style

This design system is engineered for technical proficiency, specifically optimized for API regression testing and high-density data monitoring. The brand personality is **reliable, analytical, and efficient**. It aims to evoke a sense of absolute control over complex data environments, transforming overwhelming logs into actionable insights through rigorous organization.

The visual style is **Corporate / Modern** with a strong emphasis on functional density. It utilizes a structured information hierarchy that prioritizes legibility and rapid scanning. The aesthetic is clean and utilitarian, avoiding unnecessary ornamentation to keep the user focused on status changes, error rates, and system performance. Key visual traits include:
- High-density layouts for data comparison.
- Clear, semantic color coding for immediate status recognition.
- Subtle depth to separate navigation, content containers, and analytical overlays.

## Colors

The palette is centered around a **Professional Blue** primary color, chosen for its association with trust and technical stability. The system uses a logic-driven semantic color model to communicate system health:

- **Primary (#1D4ED8):** Used for primary actions, active navigation states, and key interactive elements.
- **Success (#22C55E):** Indicates passed tests, active status, and positive growth.
- **Warning (#F59E0B):** Signals non-critical issues or pending states.
- **Error (#EF4444):** Reserved for failed tests, deleted items, and critical alerts.
- **Neutral/Secondary:** A range of slate grays is used for borders, secondary text, and background surfaces to ensure the semantic colors remain the focal point.

Backgrounds utilize a very light cool gray (`#F8FAFC`) to reduce eye strain during prolonged monitoring sessions compared to pure white.

## Typography

The typography strategy balances modern professionalism with technical utility. 

- **Hanken Grotesk** is used for headings to provide a sharp, contemporary feel that distinguishes sections clearly.
- **Inter** serves as the workhorse for all body copy and UI labels, selected for its exceptional legibility in data-heavy tables.
- **JetBrains Mono** is utilized specifically for API endpoints, JSON payloads, and Rule IDs, providing the necessary character alignment for technical review.

For mobile viewports, `display-lg` should scale down to `24px` to prevent overflow, while table-based text (`body-sm`) remains constant to ensure data integrity is maintained at smaller sizes via horizontal scrolling.

## Layout & Spacing

The layout follows a **Fixed Grid** philosophy for desktop to maintain structural consistency during data comparisons, transitioning to a fluid model for tablet and mobile.

- **Grid:** A 12-column grid is used for the main content area. Dashboard widgets typically span 3, 6, or 12 columns.
- **Sidebar:** A fixed 240px left-hand navigation provides constant access to system modules.
- **Density:** The system utilizes a "Compact-First" approach. Tables use a 48px row height for standard lists and 36px for high-density comparisons.
- **Breakpoints:** 
    - Desktop: 1280px+ (Full sidebar + 12 col grid)
    - Tablet: 768px - 1279px (Collapsed sidebar, 8 col grid)
    - Mobile: Below 768px (Bottom nav or Hamburger, single column stack)

## Elevation & Depth

This design system uses **Tonal Layers** and **Low-Contrast Outlines** to define depth without visual clutter.

- **Level 0 (Background):** Neutral base gray (`#F8FAFC`).
- **Level 1 (Cards/Containers):** Pure white surfaces with a 1px solid border (`#E2E8F0`). No shadows are used for standard containers to keep the interface "flat" and fast.
- **Level 2 (Dropdowns/Modals):** Pure white surfaces with a soft ambient shadow (Blur: 12px, Opacity: 8%, Color: Slate-900) to indicate temporary overlay.
- **Separators:** 1px lines in `#F1F5F9` are used within cards to divide headers from content and separate table rows.

## Shapes

The shape language is **Soft** and disciplined. A 4px (`0.25rem`) corner radius is the standard for almost all UI elements including buttons, input fields, and containers. This slight rounding takes the "edge" off the technical interface while maintaining a sense of precision.

- **Buttons & Inputs:** 4px radius.
- **Status Chips:** 100px (fully rounded) to differentiate them from interactive buttons.
- **Main Containers:** 8px (`0.5rem`) for larger dashboard cards to provide a clear framing effect.

## Components

### Buttons
- **Primary:** Solid Blue (`#1D4ED8`) with white text.
- **Secondary:** White background with `#E2E8F0` border and Slate-700 text.
- **Ghost:** No background/border, Primary Blue text, used for secondary actions like "Edit" or "Delete" in tables.

### Status Indicators (Chips)
Small, pill-shaped badges with low-opacity backgrounds and high-contrast text:
- **Passed/Active:** Light green background, dark green text.
- **Failed/Error:** Light red background, dark red text.
- **Warning:** Light amber background, dark amber text.

### Data Tables
Tables are the core of the system.
- **Headers:** Light gray background (`#F8FAFC`), uppercase bold text (`label-xs`).
- **Cells:** Standard `body-sm` text. Align numeric data to the right.
- **Hover State:** Rows highlight with a subtle `#F1F5F9` background.

### Input Fields
- **Standard:** 1px border (`#CBD5E1`), 12px horizontal padding. Focus state uses a 2px Primary Blue ring with 0% offset.
- **Search:** Includes a leading magnifying glass icon in Slate-400.

### Progress Bars
Used in comparison views. A dual-tone bar where the total width represents 100%, and colored segments (Green/Red/Orange) represent the percentage of each status.