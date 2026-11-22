import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { LoginRequest } from '../../../core/auth/auth.models';

@Component({
  selector: 'app-login-form',
  imports: [ReactiveFormsModule],
  templateUrl: './login-form.component.html',
  styleUrl: './login-form.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginFormComponent {
  @Input() submitting = false;
  @Output() readonly loginRequested = new EventEmitter<LoginRequest>();

  protected readonly form = new FormGroup({
    userName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    password: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  protected submit(): void {
    if (this.submitting || this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loginRequested.emit(this.form.getRawValue());
  }
}
