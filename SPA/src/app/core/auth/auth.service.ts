import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of, throwError } from 'rxjs';
import {
  InitialCredentials,
  LoginRequest,
  LoginResult,
  PasswordChange,
} from './auth.models';

const failedLogin: LoginResult = { succeeded: false, mustChangePassword: false };

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly path = '/api/auth';

  /** Wrong credentials come back as a failed result, not as an error. */
  login(request: LoginRequest): Observable<LoginResult> {
    return this.http.post<LoginResult>(`${this.path}/login`, request).pipe(
      catchError((error: unknown) =>
        error instanceof HttpErrorResponse && error.status === 401
          ? of(failedLogin)
          : throwError(() => error),
      ),
    );
  }

  setInitialCredentials(value: InitialCredentials): Observable<LoginResult> {
    return this.http.post<LoginResult>(`${this.path}/initial-credentials`, {
      ...value,
      newUserName: value.newUserName?.trim() || null,
    });
  }

  changePassword(value: PasswordChange): Observable<void> {
    return this.http
      .post<unknown>(`${this.path}/password`, value)
      .pipe(map(() => undefined));
  }
}
