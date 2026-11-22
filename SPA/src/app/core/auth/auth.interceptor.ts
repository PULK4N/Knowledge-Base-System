import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { isLoginUrl } from './return-url';

const loginRequestPath = '/api/auth/login';

/**
 * Sends the user to the login page when the API answers 401, and back to the current page after
 * login. A 401 from the login request itself means wrong credentials and is left to the login page.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const router = inject(Router);

  return next(request).pipe(
    catchError((error: unknown) => {
      if (
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        !request.url.endsWith(loginRequestPath) &&
        !isLoginUrl(router.url)
      ) {
        void router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
      }
      return throwError(() => error);
    }),
  );
};
