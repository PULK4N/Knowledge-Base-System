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
