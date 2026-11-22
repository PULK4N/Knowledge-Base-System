import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { SessionKeyState } from '../feature/session-key-state';

type OpenSessionKeyState = Exclude<SessionKeyState, { readonly status: 'closed' }>;

/** Shows a generated session key once, ready to put in the agent's environment as KB_API_KEY. */
@Component({
  selector: 'app-session-key-dialog',
  templateUrl: './session-key-dialog.component.html',
  styleUrl: './session-key-dialog.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SessionKeyDialogComponent {
  @Input({ required: true }) set state(value: OpenSessionKeyState) {
    this.current = value;
    this.copied.set(false);
  }
  @Output() readonly closed = new EventEmitter<void>();

  protected current!: OpenSessionKeyState;
  protected readonly copied = signal(false);

  protected environmentLine(sessionKey: string): string {
    return `KB_API_KEY=${sessionKey}`;
  }

  protected copy(sessionKey: string): void {
    void navigator.clipboard
      .writeText(this.environmentLine(sessionKey))
      .then(() => this.copied.set(true))
      .catch(() => this.copied.set(false));
  }
}
