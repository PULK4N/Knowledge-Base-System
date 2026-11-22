import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
} from '@angular/forms';
import { newPasswordError } from '../../../core/auth/password-rules';

export interface NewPassword {
  readonly newPassword: string;
  readonly confirmPassword: string;
}

function validNewPassword(group: AbstractControl): ValidationErrors | null {
  const { newPassword, confirmPassword } = group.value as NewPassword;
  const message = newPasswordError(newPassword, confirmPassword);
  return message ? { newPassword: message } : null;
}

@Component({
  selector: 'app-new-password-form',
  imports: [ReactiveFormsModule],
  templateUrl: './new-password-form.component.html',
  styleUrl: './new-password-form.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewPasswordFormComponent {
  @Input() submitting = false;
  @Output() readonly passwordRequested = new EventEmitter<NewPassword>();

  protected readonly form = new FormGroup(
    {
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
    this.passwordRequested.emit(this.form.getRawValue());
  }
}
