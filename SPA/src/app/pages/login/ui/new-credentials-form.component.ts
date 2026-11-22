import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
} from '@angular/forms';
import { newPasswordError } from '../../../core/auth/password-rules';

export interface NewCredentials {
  readonly newUserName: string;
  readonly newPassword: string;
  readonly confirmPassword: string;
}

function validNewPassword(group: AbstractControl): ValidationErrors | null {
  const { newPassword, confirmPassword } = group.value as NewCredentials;
  const message = newPasswordError(newPassword, confirmPassword);
  return message ? { newPassword: message } : null;
}

@Component({
  selector: 'app-new-credentials-form',
  imports: [ReactiveFormsModule],
  templateUrl: './new-credentials-form.component.html',
  styleUrl: './new-credentials-form.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewCredentialsFormComponent {
  @Input() submitting = false;
  @Input({ required: true }) set userName(value: string) {
    this.form.controls.newUserName.setValue(value);
  }
  @Output() readonly credentialsRequested = new EventEmitter<NewCredentials>();

  protected readonly form = new FormGroup(
    {
      newUserName: new FormControl('', { nonNullable: true }),
      newPassword: new FormControl('', { nonNullable: true }),
      confirmPassword: new FormControl('', { nonNullable: true }),
    },
    { validators: validNewPassword },
  );

  protected submit(): void {
    if (this.submitting || this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.credentialsRequested.emit(this.form.getRawValue());
  }
}
