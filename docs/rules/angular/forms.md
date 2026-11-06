# Purpose

Standardize form handling so inputs are type-safe, validated consistently, and aligned with
backend validation.

# Rules

- Use **typed Reactive Forms** (`FormGroup`/`FormControl` with explicit types); avoid template-driven forms for non-trivial forms.
- Build forms with `inject(FormBuilder)` (or `NonNullableFormBuilder`) and strong types — never `any`.

## Validation

- Define validators declaratively on controls; extract reusable/custom validators into shared functions (in `shared/`, exported via the barrel).
- Mirror backend FluentValidation rules on the client for UX, but treat the **server as the source of truth**.
- Display a field's error only after it is `touched`/`dirty` or after a submit attempt — never on a pristine, untouched control.
- Centralize error messages in a typed map (key → message); do not scatter literal strings across templates.
- Show one clear message per control via Material `mat-error`; bind to `control.errors` keys.
- Disable the submit button while the form is `invalid` or `pending` (async validators in flight).
- Use async validators (debounced) for server-side checks like uniqueness; show the pending state.
- On submit failure, map backend `ProblemDetails`/validation errors back onto the matching controls with `setErrors`, and show a summary toast (see notifications rules).

## Orchestration

- Keep form-building and submission orchestration in services where logic is reusable; components only bind and present.
- On success, show a success toast and reset/navigate as appropriate; on failure, surface the error via field errors + a toast.

# AI Generation Requirements

When generating forms, the AI **must**:

- Use strongly typed Reactive Forms with declarative validators and a centralized error-message map.
- Gate error display on touched/dirty/submitted and disable submit while invalid or pending.
- Map server-side `ProblemDetails` errors onto the matching controls and notify via toast.

The AI **must never**:

- Use untyped forms or `any` for form models.
- Rely solely on client validation as if it were authoritative.
- Show validation errors on pristine controls or hardcode error strings in templates.
