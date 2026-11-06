import { AbstractControl } from '@angular/forms';

/**
 * First validation message for a control, gated on touched/dirty (so errors don't
 * show on a pristine field). Returns null when valid or not yet interacted with.
 * Centralizes error wording so messages aren't scattered across templates.
 */
export function controlErrorMessage(control: AbstractControl | null | undefined, label: string): string | null {
  if (!control || !control.errors || (!control.touched && !control.dirty)) {
    return null;
  }

  const errors = control.errors;
  if (errors['required']) {
    return `${label} is required.`;
  }
  if (errors['maxlength']) {
    return `${label} must be at most ${errors['maxlength'].requiredLength} characters.`;
  }
  return `${label} is invalid.`;
}
