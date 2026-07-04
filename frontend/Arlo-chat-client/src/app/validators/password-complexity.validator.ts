import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const PASSWORD_MIN_LENGTH = 8;
export const PASSWORD_MAX_LENGTH = 10;

/**
 * Client-side mirror of the backend's PasswordValidator, for instant feedback only.
 * The server re-checks everything; this never substitutes for that check.
 */
export const passwordComplexityValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const value = (control.value as string) ?? '';
  if (!value) {
    return null; // let Validators.required own the empty case
  }

  if (value.length < PASSWORD_MIN_LENGTH || value.length > PASSWORD_MAX_LENGTH) {
    return { passwordLength: true };
  }
  if (!/[A-Za-z]/.test(value)) {
    return { passwordLetter: true };
  }
  if (!/[0-9]/.test(value)) {
    return { passwordDigit: true };
  }
  if (!/[^A-Za-z0-9]/.test(value)) {
    return { passwordSymbol: true };
  }

  return null;
};
