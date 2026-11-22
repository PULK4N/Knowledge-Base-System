import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import {
  Observable,
  Subject,
  catchError,
  exhaustMap,
  map,
  merge,
  of,
  scan,
  shareReplay,
  startWith,
  tap,
} from 'rxjs';
import { LoginRequest } from '../../../core/auth/auth.models';
import { AuthService } from '../../../core/auth/auth.service';
import { toUserMessage } from '../../../core/http/load-state';
import { LoginFormComponent } from '../ui/login-form.component';
import { NewCredentials, NewCredentialsFormComponent } from '../ui/new-credentials-form.component';
import { LoginEvent, LoginState, initialLoginState, reduceLogin } from './login-state';

interface NewCredentialsRequest extends NewCredentials {
  readonly currentPassword: string;
}

@Component({
  selector: 'app-login-page',
  imports: [AsyncPipe, LoginFormComponent, NewCredentialsFormComponent],
  templateUrl: './login.page.html',
  styleUrl: './login.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly loginRequests = new Subject<LoginRequest>();
  private readonly newCredentialsRequests = new Subject<NewCredentialsRequest>();

  private readonly login$: Observable<LoginEvent> = this.loginRequests.pipe(
    exhaustMap(request => this.auth.login(request).pipe(
      map(result => ({ type: 'login-completed', ...request, result }) as LoginEvent),
      startWith({ type: 'submitted' } as LoginEvent),
      catchError(error => of({ type: 'failed', message: toUserMessage(error) } as LoginEvent)),
    )),
  );

  private readonly newCredentials$: Observable<LoginEvent> = this.newCredentialsRequests.pipe(
    exhaustMap(request => this.auth.setInitialCredentials({
      currentPassword: request.currentPassword,
      newUserName: request.newUserName,
      newPassword: request.newPassword,
      confirmPassword: request.confirmPassword,
    }).pipe(
      map(() => ({ type: 'new-password-saved' }) as LoginEvent),
      startWith({ type: 'submitted' } as LoginEvent),
      catchError(() => of({
        type: 'failed',
        message: 'The new password could not be saved. Please try again.',
      } as LoginEvent)),
    )),
  );

  protected readonly state$: Observable<LoginState> = merge(this.login$, this.newCredentials$).pipe(
    scan(reduceLogin, initialLoginState),
    startWith(initialLoginState),
    tap(state => {
      if (state.step === 'done') void this.router.navigateByUrl('/');
    }),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  protected login(request: LoginRequest): void {
    this.loginRequests.next(request);
  }

  protected saveNewCredentials(currentPassword: string, value: NewCredentials): void {
    this.newCredentialsRequests.next({ ...value, currentPassword });
  }
}
