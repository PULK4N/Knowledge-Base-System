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
  BehaviorSubject,
  Observable,
  Subject,
  catchError,
  combineLatest,
  debounceTime,
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
  Policy,
  PolicyScope,
  PolicySearchRequest,
  PolicySearchResult,
} from '../data-access/policy.models';
import { PolicyService } from '../data-access/policy.service';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';

interface PolicyListView {
  readonly scope: PolicyScope;
  readonly title: string;
  readonly subtitle: string;
  readonly backLink: string | null;
  readonly backLabel: string | null;
  readonly topicNames: readonly string[];
  readonly result: PolicySearchResult;
}

type PolicyAction =
  | {
      readonly kind: 'update';
      readonly scope: PolicyScope;
      readonly policy: Policy;
    }
  | {
      readonly kind: 'remove';
      readonly scope: PolicyScope;
      readonly policyId: string;
    };

type PolicyMutationState =
  | { readonly status: 'idle' }
  | {
      readonly status: 'saving' | 'deleting';
      readonly policyId: string;
    }
  | {
      readonly status: 'error';
      readonly policyId: string;
      readonly message: string;
    };

const PAGE_SIZE = 5;

export function policyScopeFromRoute(
  scopeKind: unknown,
  params: ParamMap,
): PolicyScope | null {
  if (scopeKind === 'general') return { kind: 'general' };

  if (scopeKind === 'topic') {
    const topicName = params.get('topicName');
    return topicName ? { kind: 'topic', topicName } : null;
  }

  if (scopeKind === 'project') {
    const projectId = params.get('projectId');
    return projectId ? { kind: 'project', projectId } : null;
  }

  return null;
}

@Component({
  selector: 'app-policy-list-page',
  imports: [AsyncPipe, FormsModule, PaginationComponent, RouterLink],
  templateUrl: './policy-list.page.html',
  styleUrls: ['./policy-list.page.css', '../ui/knowledge-list.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PolicyListPage {
  private readonly route = inject(ActivatedRoute);
  private readonly policies = inject(PolicyService);
  private readonly querySubject = new BehaviorSubject<PolicySearchRequest>({
    page: 1,
    pageSize: PAGE_SIZE,
    search: '',
  });
  private readonly actions = new Subject<PolicyAction>();

  protected readonly editingPolicyId = signal<string | null>(null);
  protected readonly confirmingPolicyId = signal<string | null>(null);

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
    combineLatest({ scope: this.scope$, request: this.querySubject }).pipe(
      debounceTime(200),
      switchMap(({ scope, request }) => {
        if (!scope) {
          return of({
            status: 'error',
            message: 'The requested policy collection could not be found.',
          } as const);
        }

        return this.load(scope, request).pipe(
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

  private readonly mutation$: Observable<PolicyMutationState> =
    this.actions.pipe(
      exhaustMap(action => {
        if (action.kind === 'update') {
          return this.policies.updatePolicy(action.scope, {
            policyId: action.policy.id,
            title: action.policy.title,
            description: action.policy.description,
          }).pipe(
            tap(() => this.editingPolicyId.set(null)),
            map(() => ({ status: 'idle' }) as const),
            startWith({
              status: 'saving',
              policyId: action.policy.id,
            } as const),
            catchError(error =>
              of({
                status: 'error',
                policyId: action.policy.id,
                message: toUserMessage(error),
              } as const),
            ),
          );
        }

        return this.policies.removePolicy(action.scope, action.policyId).pipe(
          tap(() => this.confirmingPolicyId.set(null)),
          map(() => ({ status: 'idle' }) as const),
          startWith({
            status: 'deleting',
            policyId: action.policyId,
          } as const),
          catchError(error =>
            of({
              status: 'error',
              policyId: action.policyId,
              message: toUserMessage(error),
            } as const),
          ),
        );
      }),
      startWith({ status: 'idle' } as const),
    );

  protected readonly vm$ = combineLatest({
    state: this.state$,
    mutation: this.mutation$,
  }).pipe(shareReplay({ bufferSize: 1, refCount: true }));

  protected search(search: string): void {
    this.querySubject.next({
      ...this.querySubject.value,
      page: 1,
      search,
    });
  }

  protected goToPage(page: number): void {
    this.querySubject.next({ ...this.querySubject.value, page });
  }

  protected startEditing(policy: Policy): void {
    this.confirmingPolicyId.set(null);
    this.editingPolicyId.set(policy.id);
  }

  protected cancelEditing(): void {
    this.editingPolicyId.set(null);
  }

  protected updatePolicy(
    scope: PolicyScope,
    policyId: string,
    title: string,
    description: string,
  ): void {
    this.actions.next({
      kind: 'update',
      scope,
      policy: {
        id: policyId,
        title: title.trim(),
        description,
      },
    });
  }

  protected startRemoving(policyId: string): void {
    this.editingPolicyId.set(null);
    this.confirmingPolicyId.set(policyId);
  }

  protected cancelRemoving(): void {
    this.confirmingPolicyId.set(null);
  }

  protected removePolicy(scope: PolicyScope, policyId: string): void {
    this.actions.next({ kind: 'remove', scope, policyId });
  }

  private load(
    scope: PolicyScope,
    request: PolicySearchRequest,
  ): Observable<PolicyListView> {
    const policies$ = this.policies.searchPolicies(scope, request);

    if (scope.kind === 'general') {
      return policies$.pipe(
        map(result => ({
          scope,
          title: 'General Policies',
          subtitle: 'Rules that apply across every project and topic',
          backLink: null,
          backLabel: null,
          topicNames: [],
          result,
        })),
      );
    }

    if (scope.kind === 'topic') {
      return policies$.pipe(
        map(result => ({
          scope,
          title: scope.topicName,
          subtitle: 'Policies shared through this topic',
          backLink: '/topics',
          backLabel: 'Back to topics',
          topicNames: [],
          result,
        })),
      );
    }

    return combineLatest({
      result: policies$,
      project: this.policies.watchProject(scope.projectId),
    }).pipe(
      map(({ result, project }) =>
        this.projectView(scope, project, result),
      ),
    );
  }

  private projectView(
    scope: Extract<PolicyScope, { readonly kind: 'project' }>,
    project: PolicyProjectDetails,
    result: PolicySearchResult,
  ): PolicyListView {
    return {
      scope,
      title: project.name,
      subtitle:
        project.description || 'Project-specific policies and related topics',
      backLink: '/projects',
      backLabel: 'Back to projects',
      topicNames: project.topicNames,
      result,
    };
  }
}
