import { AsyncPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, ParamMap, RouterLink } from '@angular/router';
import {
  Observable,
  Subject,
  catchError,
  combineLatest,
  distinctUntilChanged,
  exhaustMap,
  map,
  of,
  shareReplay,
  startWith,
  switchMap,
  tap,
} from 'rxjs';
import { LoadState, toUserMessage } from '../../../core/http/load-state';
import {
  PolicyProjectDetails,
  PolicyScope,
} from '../data-access/policy.models';
import { PolicyService } from '../data-access/policy.service';
import { PolicyCollectionComponent } from '../ui/policy-collection.component';

interface PolicyListView {
  readonly scope: PolicyScope;
  readonly title: string;
  readonly subtitle: string;
  readonly backLink: string | null;
  readonly backLabel: string | null;
  readonly addRoute: readonly string[];
  readonly topicNames: readonly string[];
  readonly repositoryPaths: readonly string[];
}

type ProjectConnectionAction =
  | {
      readonly kind: 'repository';
      readonly projectId: string;
      readonly value: string;
    }
  | {
      readonly kind: 'topic';
      readonly projectId: string;
      readonly value: string;
    }
  | {
      readonly kind: 'remove-topic';
      readonly projectId: string;
      readonly value: string;
    };

type ProjectConnectionKind = ProjectConnectionAction['kind'];

type ProjectConnectionState =
  | { readonly status: 'idle' }
  | { readonly status: 'saving'; readonly kind: ProjectConnectionKind }
  | {
      readonly status: 'error';
      readonly kind: ProjectConnectionKind;
      readonly message: string;
    };

export function policyScopeFromRoute(
  scopeKind: unknown,
  params: ParamMap,
): PolicyScope | null {
  if (scopeKind === 'general') return { kind: 'general' };

  if (scopeKind === 'topic') {
    const topicName = params.get('topicName');
    return topicName ? { kind: 'topic', topicName } : null;
  }

  if (scopeKind === 'agentFamily') {
    const agentFamilyName = params.get('agentFamilyName');
    return agentFamilyName ? { kind: 'agentFamily', agentFamilyName } : null;
  }

  if (scopeKind === 'project') {
    const projectId = params.get('projectId');
    return projectId ? { kind: 'project', projectId } : null;
  }

  return null;
}

@Component({
  selector: 'app-policy-list-page',
  imports: [AsyncPipe, FormsModule, PolicyCollectionComponent, RouterLink],
  templateUrl: './policy-list.page.html',
  styleUrls: ['./policy-list.page.css', '../ui/knowledge-list.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PolicyListPage {
  private readonly route = inject(ActivatedRoute);
  private readonly policies = inject(PolicyService);
  private readonly connectionRequests = new Subject<ProjectConnectionAction>();

  protected readonly confirmingProjectTopicName = signal<string | null>(null);

  private readonly scope$ = combineLatest([
    this.route.data,
    this.route.paramMap,
  ]).pipe(
    map(([data, params]) => policyScopeFromRoute(data['policyScope'], params)),
    distinctUntilChanged(
      (previous, current) => JSON.stringify(previous) === JSON.stringify(current),
    ),
  );

  private readonly state$: Observable<LoadState<PolicyListView>> =
    this.scope$.pipe(
      switchMap(scope => {
        if (!scope) {
          return of({
          status: 'error',
          message: 'The requested policy collection could not be found.',
        } as const);
        }

        return this.load(scope).pipe(
        map(data => ({ status: 'success', data }) as const),
        startWith({ status: 'loading' } as const),
        catchError(error =>
          of({
            status: 'error',
            message: toUserMessage(error),
          } as const),
        ),
        );
      }),
      shareReplay({ bufferSize: 1, refCount: true }),
    );

  private readonly connectionMutation$: Observable<ProjectConnectionState> =
    this.connectionRequests.pipe(
      exhaustMap(action => {
        const request$ = this.executeConnectionAction(action);

        return request$.pipe(
        tap(() => {
          if (action.kind === 'remove-topic') {
            this.confirmingProjectTopicName.set(null);
          }
        }),
        map(() => ({ status: 'idle' }) as const),
        startWith({ status: 'saving', kind: action.kind } as const),
        catchError(error =>
          of({
            status: 'error',
            kind: action.kind,
            message: toUserMessage(error),
          } as const),
        ),
        );
      }),
      startWith({ status: 'idle' } as const),
    );

  protected readonly vm$ = combineLatest({
    state: this.state$,
    connectionMutation: this.connectionMutation$,
  }).pipe(shareReplay({ bufferSize: 1, refCount: true }));

  protected addProjectRepository(projectId: string, repositoryPath: string): void {
    const value = repositoryPath.trim();
    if (!value) return;

    this.connectionRequests.next({ kind: 'repository', projectId, value });
  }

  protected addProjectTopic(projectId: string, topicName: string): void {
    const value = topicName.trim();
    if (!value) return;

    this.connectionRequests.next({ kind: 'topic', projectId, value });
  }

  protected startRemovingProjectTopic(topicName: string): void {
    this.confirmingProjectTopicName.set(topicName);
  }

  protected cancelRemovingProjectTopic(): void {
    this.confirmingProjectTopicName.set(null);
  }

  protected removeProjectTopic(projectId: string, topicName: string): void {
    this.connectionRequests.next({
      kind: 'remove-topic',
      projectId,
      value: topicName,
    });
  }

  private executeConnectionAction(
    action: ProjectConnectionAction,
  ): Observable<PolicyProjectDetails> {
    switch (action.kind) {
      case 'repository':
        return this.policies.addProjectRepository(
        action.projectId,
        action.value,
        );
      case 'topic':
        return this.policies.addProjectTopic(action.projectId, action.value);
      case 'remove-topic':
        return this.policies.removeProjectTopic(
        action.projectId,
        action.value,
        );
    }
  }

  private load(scope: PolicyScope): Observable<PolicyListView> {
    if (scope.kind === 'general') {
      return of({
        scope,
        title: 'General Policies',
        subtitle: 'Rules that apply across every project and topic',
        backLink: null,
        backLabel: null,
        addRoute: ['/policies', 'general', 'new'],
        topicNames: [],
        repositoryPaths: [],
      });
    }

    if (scope.kind === 'topic') {
      return of({
        scope,
        title: scope.topicName,
        subtitle: 'Policies shared through this topic',
        backLink: '/policies/topics',
        backLabel: 'Back to topics',
        addRoute: [
          '/policies',
          'topics',
          scope.topicName,
          'policies',
          'new',
        ],
        topicNames: [],
        repositoryPaths: [],
      });
    }

    if (scope.kind === 'agentFamily') {
      return of({
        scope,
        title: scope.agentFamilyName,
        subtitle: 'Policies applied only to this kind of agent',
        backLink: '/policies/agent-families',
        backLabel: 'Back to agent families',
        addRoute: [
          '/policies',
          'agent-families',
          scope.agentFamilyName,
          'policies',
          'new',
        ],
        topicNames: [],
        repositoryPaths: [],
      });
    }

    return this.policies
      .watchProject(scope.projectId)
      .pipe(map(project => this.projectView(scope, project)));
  }

  private projectView(
    scope: Extract<PolicyScope, { readonly kind: 'project' }>,
    project: PolicyProjectDetails,
  ): PolicyListView {
    return {
      scope,
      title: project.name,
      subtitle:
        project.description || 'Project-specific policies and related topics',
      backLink: '/policies/projects',
      backLabel: 'Back to projects',
      addRoute: [
        '/policies',
        'projects',
        scope.projectId,
        'policies',
        'new',
      ],
      topicNames: project.topicNames,
      repositoryPaths: project.repositoryPaths,
    };
  }
}
