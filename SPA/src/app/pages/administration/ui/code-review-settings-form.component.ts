import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CodeReviewSettings } from '../data-access/code-review-settings.models';

@Component({
  selector: 'app-code-review-settings-form',
  imports: [ReactiveFormsModule],
  templateUrl: './code-review-settings-form.component.html',
  styleUrl: './code-review-settings-form.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CodeReviewSettingsFormComponent {
  @Input() saving = false;
  @Input({ required: true }) set settings(value: CodeReviewSettings) {
    this.form.reset({ batchSize: value.batchSize, taskMessage: value.taskMessage });
  }
  @Output() readonly saveRequested = new EventEmitter<CodeReviewSettings>();

  protected readonly form = new FormGroup({
    batchSize: new FormControl(5, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(1), Validators.max(2147483647),
        control => Number.isInteger(control.value) ? null : { integer: true }],
    }),
    taskMessage: new FormControl('', {
      nonNullable: true,
      validators: [control => typeof control.value === 'string' && control.value.trim() ? null : { required: true }],
    }),
  });

  protected submit(): void {
    if (this.saving || this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saveRequested.emit(this.form.getRawValue());
  }
}
