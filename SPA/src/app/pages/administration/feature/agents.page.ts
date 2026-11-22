import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import {
  Observable,
  Subject,
  catchError,
  combineLatest,
  exhaustMap,
  map,
  merge,
  of,
  scan,
  shareReplay,
  startWith,
  switchMap,
} from 'rxjs';
import { LoadState, toUserMessage } from '../../../core/http/load-state';
import { Agent, AgentDraft } from '../data-access/agents.models';
import { AgentsService } from '../data-access/agents.service';
import { AgentFormComponent } from '../ui/agent-form.component';
import { AgentListComponent } from '../ui/agent-list.component';
import { AgentsEvent, initialAgentsState, reduceAgents } from './agents-state';

interface SaveRequest {
  readonly agent: Agent | null;
  readonly draft: AgentDraft;
}

@Component({
  selector: 'app-agents-page',
  imports: [AsyncPipe, AgentFormComponent, AgentListComponent],
  templateUrl: './agents.page.html',
  styleUrl: './agents.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AgentsPage {
  private readonly agents = inject(AgentsService);
  private readonly reloadRequests = new Subject<void>();
  private readonly editorEvents = new Subject<AgentsEvent>();
  private readonly saveRequests = new Subject<SaveRequest>();

  private readonly load$ = this.reloadRequests.pipe(
    startWith(undefined),
    switchMap(() => this.agents.watchAll().pipe(
      map(data => ({ status: 'success', data }) as LoadState<readonly Agent[]>),
      startWith({ status: 'loading' } as LoadState<readonly Agent[]>),
      catchError(error => of({ status: 'error', message: toUserMessage(error) } as LoadState<readonly Agent[]>)),
    )),
  );

  private readonly save$: Observable<AgentsEvent> = this.saveRequests.pipe(
    exhaustMap(({ agent, draft }) => {
      const save$: Observable<AgentsEvent> = agent
        ? this.agents.setPermissions(agent.id, draft).pipe(
            map(saved => ({ type: 'permissions-saved', agent: saved }) as AgentsEvent),
          )
        : this.agents.create(draft).pipe(
            map(created => ({ type: 'created', agent: created }) as AgentsEvent),
          );
      return save$.pipe(
        startWith({ type: 'submitted' } as AgentsEvent),
        catchError(() => of({
          type: 'failed',
          message: agent
            ? 'The permissions could not be saved. Please try again.'
            : 'The agent could not be created. The username may already be taken.',
        } as AgentsEvent)),
      );
    }),
  );

  private readonly state$ = merge(this.editorEvents, this.save$).pipe(
    scan(reduceAgents, initialAgentsState),
    startWith(initialAgentsState),
  );

  protected readonly vm$ = combineLatest({ load: this.load$, state: this.state$ }).pipe(
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  protected reload(): void { this.reloadRequests.next(); }
  protected openCreate(): void { this.editorEvents.next({ type: 'create-opened' }); }
  protected openEdit(agent: Agent): void { this.editorEvents.next({ type: 'edit-opened', agent }); }
  protected closeEditor(): void { this.editorEvents.next({ type: 'editor-closed' }); }
  protected save(agent: Agent | null, draft: AgentDraft): void { this.saveRequests.next({ agent, draft }); }
}
