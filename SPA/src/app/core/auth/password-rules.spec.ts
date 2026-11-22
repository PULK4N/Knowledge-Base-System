import { newPasswordError } from './password-rules';

describe('newPasswordError', () => {
  it.each([
    ['', '', 'Enter a new password.'],
    ['   ', '   ', 'Enter a new password.'],
    ['123', '123', 'Choose a password other than the initial one.'],
    ['secret', 'other', 'The passwords do not match.'],
    ['secret', 'secret', null],
  ])('returns the reason for %j / %j', (newPassword, confirmPassword, expected) => {
    expect(newPasswordError(newPassword, confirmPassword)).toBe(expected);
  });
});
