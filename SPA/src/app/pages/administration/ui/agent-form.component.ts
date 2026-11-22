import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  PERMISSION_AREAS,
  permissionLabel,
  togglePermission,
} from '../data-access/agent-permissions';
import { Agent, AgentDraft } from '../data-access/agents.models';

/** Creates an agent, or edits an existing agent's permissions when an agent is given. */
@Component({
  selector: 'app-agent-form',
  imports: [ReactiveFormsModule],
  templateUrl: './agent-form.component.html',
  styleUrl: './agent-form.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AgentFormComponent {
  @Input() saving = false;
  @Input() set agent(value: Agent | null) {
    this.editedAgent = value;
    this.form.reset({
      userName: value?.userName ?? '',
      displayName: value?.displayName ?? '',
      isAdministrator: value?.isAdministrator ?? false,
      permissions: [...(value?.permissions ?? [])],
    });
  }
  @Output() readonly saved = new EventEmitter<AgentDraft>();
  @Output() readonly cancelled = new EventEmitter<void>();

  protected editedAgent: Agent | null = null;
  protected readonly areas = PERMISSION_AREAS;
  protected readonly label = permissionLabel;

  protected readonly form = new FormGroup({
    userName: new FormControl('', {
      nonNullable: true,
      validators: [control => this.editedAgent || String(control.value).trim() ? null : { required: true }],
    }),
    displayName: new FormControl('', { nonNullable: true }),
    isAdministrator: new FormControl(false, { nonNullable: true }),
    permissions: new FormControl<string[]>([], { nonNullable: true, validators: [Validators.required] }),
  });

  protected isChecked(permission: string): boolean {
    return this.form.controls.permissions.value.includes(permission);
  }

  protected toggle(permission: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    const permissions = this.form.controls.permissions;
    permissions.setValue(togglePermission(permissions.value, permission, checked));
    permissions.markAsTouched();
  }

  protected submit(): void {
    const value = this.form.getRawValue();
    const hasAccess = value.isAdministrator || value.permissions.length > 0;
    if (this.saving || this.form.controls.userName.invalid || !hasAccess) {
      this.form.markAllAsTouched();
      return;
    }
    this.saved.emit(value);
  }
}
