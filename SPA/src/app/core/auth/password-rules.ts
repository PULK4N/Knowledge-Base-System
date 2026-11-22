/** The initial administrator password, accepted only until it is changed. */
export const INITIAL_PASSWORD = '123';

/** The reason a new password cannot be used, or null when it is valid. */
export function newPasswordError(
  newPassword: string,
  confirmPassword: string,
): string | null {
  if (!newPassword.trim()) return 'Enter a new password.';
  if (newPassword === INITIAL_PASSWORD) {
    return 'Choose a password other than the initial one.';
  }
  if (newPassword !== confirmPassword) return 'The passwords do not match.';
  return null;
}
