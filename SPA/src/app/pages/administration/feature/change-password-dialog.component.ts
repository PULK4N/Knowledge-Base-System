import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
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
} from 'rxjs';
import { PasswordChange } from '../../../core/auth/auth.models';
import { AuthService } from '../../../core/auth/auth.service';
import { ChangePasswordFormComponent } from '../ui/change-password-form.component';
import {
  ChangePasswordEvent,
  ChangePasswordState,
  closedChangePassword,
  reduceChangePassword,
} from './change-password-state';

/** The Users page button that opens a dialog for changing the administrator's password. */
@Component({
  selector: 'app-change-password-dialog',
  imports: [AsyncPipe, ChangePasswordFormComponent],
  templateUrl: './change-password-dialog.component.html',
  styleUrl: './change-password-dialog.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChangePasswordDialogComponent {
  private readonly auth = inject(AuthService);
  private readonly visibility = new Subject<ChangePasswordEvent>();
  private readonly changeRequests = new Subject<PasswordChange>();

  private readonly save$: Observable<ChangePasswordEvent> = this.changeRequests.pipe(
    exhaustMap(change => this.auth.changePassword(change).pipe(
      map(() => ({ type: 'saved' }) as ChangePasswordEvent),
      startWith({ type: 'submitted' } as ChangePasswordEvent),
      catchError(() => of({
        type: 'failed',
        message: 'The password was not changed. Check the current password and try again.',
      } as ChangePasswordEvent)),
    )),
  );

  protected readonly state$: Observable<ChangePasswordState> = merge(this.visibility, this.save$).pipe(
    scan(reduceChangePassword, closedChangePassword),
    startWith(closedChangePassword),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  protected open(): void { this.visibility.next({ type: 'opened' }); }
  protected close(): void { this.visibility.next({ type: 'closed' }); }
  protected change(value: PasswordChange): void { this.changeRequests.next(value); }
}
