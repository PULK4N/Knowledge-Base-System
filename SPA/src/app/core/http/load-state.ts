import { HttpErrorResponse } from '@angular/common/http';

export type LoadState<T> =
  | { readonly status: 'loading' }
  | { readonly status: 'success'; readonly data: T }
  | { readonly status: 'error'; readonly message: string };

export function toUserMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) {
      return 'The service could not be reached. Check that the API is running.';
    }

    if (error.status === 404) {
      return 'The requested item could not be found.';
    }
  }

  return 'Something went wrong while loading this page. Please try again.';
}

/**
 * The message for a failed command: the validator reasons the API reports
 * (lines after "Reason:", for example which definitions of done block a plan
 * from completing), otherwise the general message.
 */
export function toCommandErrorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse && typeof error.error === 'string') {
    const reasons = [...error.error.matchAll(/Reason:\s*([\s\S]*?)(?=\r?\n\s+at |\r?\nValidatorName:|$)/g)]
      .map(match => match[1].trim())
      .filter(reason => reason.length > 0);
    if (reasons.length > 0) return reasons.join(' ');
  }

  return toUserMessage(error);
}
