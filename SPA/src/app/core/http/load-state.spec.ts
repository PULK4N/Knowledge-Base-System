import { HttpErrorResponse } from '@angular/common/http';
import { toCommandErrorMessage } from './load-state';

describe('toCommandErrorMessage', () => {
  it('shows the validator reasons reported by the API', () => {
    const error = new HttpErrorResponse({
      status: 500,
      error: 'EventValidationException: Event validation failed with 1 failure(s):\nValidatorName: X.\nReason: \n'
        + 'The plan cannot be completed. Confirm or move to another plan: DOD-2 (NotConfirmed).\n   at Some.Method()',
    });

    expect(toCommandErrorMessage(error)).toBe(
      'The plan cannot be completed. Confirm or move to another plan: DOD-2 (NotConfirmed).',
    );
  });

  it.each([
    new HttpErrorResponse({ status: 500, error: null }),
    new Error('other'),
  ])('falls back to the general message', error => {
    expect(toCommandErrorMessage(error)).toBe('Something went wrong while loading this page. Please try again.');
  });
});
