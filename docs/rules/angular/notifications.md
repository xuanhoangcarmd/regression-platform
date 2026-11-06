# Purpose

Standardize user notifications (toast / snackbar) so success, error, warning, and info feedback
is consistent, centralized, and never leaks raw technical details to users.

# Rules

- Show transient feedback as **toast notifications**, not `alert()`/`console.log` or inline ad-hoc banners.
- Centralize toasts behind a single `NotificationService` (in `core/`, exported via the barrel) wrapping **Angular Material `MatSnackBar`** (already in the stack — no extra dependency); components/services call `notify.success()/error()/warning()/info()` — never `MatSnackBar` directly.
- Configure defaults once at app bootstrap via `MAT_SNACK_BAR_DEFAULT_OPTIONS` (position, duration, dismiss); do not pass layout/config at each call site.
- Use the correct severity: success (completed actions), error (failures), warning (risky/partial), info (neutral).
- Trigger toasts for: successful create/update/delete, save/submit failures, and background job/run/compare outcomes.
- Show **user-friendly messages**; never display stack traces, raw exceptions, or internal codes. Map backend `ProblemDetails.title`/`detail` to a readable message.
- Centralize HTTP-error toasts in the error interceptor so each call site doesn't repeat error handling; let services add context only when needed.
- Keep messages short and actionable; localize/centralize message text rather than hardcoding strings throughout.
- Do not toast on every routine read; reserve toasts for actions and failures to avoid noise.

# AI Generation Requirements

When generating user feedback, the AI **must**:

- Use the central `NotificationService` with the correct severity for the outcome.
- Map backend `ProblemDetails` to user-friendly messages and handle HTTP errors via the interceptor.
- Toast on action success/failure (create/update/delete, submit, job outcomes).

The AI **must never**:

- Call `MatSnackBar` directly from components or duplicate per-call config.
- Show raw exceptions, stack traces, or internal error codes to users.
- Use `alert()`/`console.log` for user-facing feedback or toast on routine reads.
