# Purpose

Define standards for reusable UI components so they stay generic, configurable, accessible,
and independent of any feature.

# Rules

## Location & criteria

- All reusable components live in `shared/components`.
- Promote a component to `shared/` only when it is: used by 2+ features, generic in meaning, and has no feature-specific dependency.
- Examples: `button/`, `input/`, `textarea/`, `select/`, `combobox/`, `datepicker/`, `modal/`, `confirm-dialog/`, `loading/`, `toastr/`, `data-table/`, `empty-state/`, `page-header/`.

## Responsibilities

- Shared components **should**: focus on UI, be configurable, and be reusable.
- Shared components **should NOT**: call APIs, access feature stores, or depend on feature models.

## Inputs & outputs

- Expose configuration via `input()` (e.g. `label`, `placeholder`, `disabled`, `required`, `options`).
- Emit events via `output()` (e.g. `valueChanged`, `confirmed`, `cancelled`, `selected`).

## Styling & UX

- Follow the design system; style with Tailwind utilities (see styling rules).
- Support responsive layouts and dark mode where applicable.
- Support accessibility (labels, roles, keyboard interaction) and loading/disabled states.

## Independence

- Components carrying feature-specific business concepts (e.g. Compare, Run, Project, ApiDefinition) stay in `features/*/components` — never move them into `shared/`.

# AI Generation Requirements

When generating shared components, the AI **must**:

- Build generic, configurable, standalone components driven by `input()`/`output()`.
- Support accessibility plus loading and disabled states, and keep them independent from features.

The AI **must never**:

- Call APIs, access feature stores, or import feature models from a shared component.
- Place feature-specific (business) components into `shared/`.
