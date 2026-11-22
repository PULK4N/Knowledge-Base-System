import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { PasswordChange } from '../../../core/auth/auth.models';
import { newPasswordError } from '../../../core/auth/password-rules';

function validNewPassword(group: AbstractControl): ValidationErrors | null {
  const { newPassword, confirmPassword } = group.value as PasswordChange;
  const message = newPasswordError(newPassword, confirmPassword);
  return message ? { newPassword: message } : null;
}

@Component({
  selector: 'app-change-password-form',
  imports: [ReactiveFormsModule],
  templateUrl: './change-password-form.component.html',
  styleUrl: './change-password-form.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChangePasswordFormComponent {
  @Input() saving = false;
  @Output() readonly changeRequested = new EventEmitter<PasswordChange>();
  @Output() readonly cancelled = new EventEmitter<void>();

  protected readonly form = new FormGroup(
    {
      currentPassword: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      newPassword: new FormControl('', { nonNullable: true }),
      confirmPassword: new FormControl('', { nonNullable: true }),
    },
    { validators: validNewPassword },
  );

  protected submit(): void {
    if (this.saving || this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.changeRequested.emit(this.form.getRawValue());
  }
}
