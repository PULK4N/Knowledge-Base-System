import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { Agent } from '../data-access/agents.models';

@Component({
  selector: 'app-agent-list',
  templateUrl: './agent-list.component.html',
  styleUrl: './agent-list.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AgentListComponent {
  @Input({ required: true }) agents: readonly Agent[] = [];
  @Input() disabled = false;
  @Output() readonly editRequested = new EventEmitter<Agent>();
  @Output() readonly keyRequested = new EventEmitter<Agent>();
}
