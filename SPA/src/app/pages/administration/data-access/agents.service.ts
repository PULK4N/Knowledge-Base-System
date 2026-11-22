import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import {
  Observable,
  filter,
  ignoreElements,
  map,
  merge,
  switchMap,
  tap,
} from 'rxjs';
import { EntityStore, PagedResult } from '../../../core/store/entity-store.service';
import { Agent, AgentDraft, AgentPermissions } from './agents.models';

const AGENT_ENTITY_TYPE = 'agent';
const AGENTS_QUERY_KEY = 'agents';

@Injectable({ providedIn: 'root' })
export class AgentsService {
  private readonly http = inject(HttpClient);
  private readonly store = inject(EntityStore);
  private readonly path = '/api/agents';

  /** Cached agents first, then the list from the API, newest first. */
  watchAll(): Observable<readonly Agent[]> {
    const cached$ = this.store.search$<Agent>(AGENTS_QUERY_KEY).pipe(
      filter((result): result is PagedResult<Agent> => result !== undefined),
      map(result => result.items),
    );
    return merge(cached$, this.refresh().pipe(ignoreElements()));
  }

  /** Creates the agent, then reloads the list so the new agent appears in it. */
  create(draft: AgentDraft): Observable<Agent> {
    return this.http
      .post<Agent>(this.path, {
        userName: draft.userName.trim(),
        displayName: draft.displayName.trim() || null,
        isAdministrator: draft.isAdministrator,
        permissions: draft.permissions,
      })
      .pipe(
        tap(agent => this.store.upsert(AGENT_ENTITY_TYPE, agent)),
        switchMap(agent => this.refresh().pipe(map(() => agent))),
      );
  }

  setPermissions(agentId: string, value: AgentPermissions): Observable<Agent> {
    return this.http
      .put<Agent>(`${this.path}/${encodeURIComponent(agentId)}/permissions`, {
        isAdministrator: value.isAdministrator,
        permissions: value.permissions,
      })
      .pipe(tap(agent => this.store.upsert(AGENT_ENTITY_TYPE, agent)));
  }

  private refresh(): Observable<readonly Agent[]> {
    return this.http.get<Agent[]>(this.path).pipe(
      tap(agents =>
        this.store.replaceSearch(AGENTS_QUERY_KEY, AGENT_ENTITY_TYPE, {
          items: agents,
          page: 1,
          pageSize: agents.length,
          totalCount: agents.length,
          totalPages: agents.length === 0 ? 0 : 1,
          hasPreviousPage: false,
          hasNextPage: false,
        }),
      ),
    );
  }
}
