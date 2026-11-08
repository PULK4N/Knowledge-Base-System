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
  Policy,
  PolicyScope,
  PolicySearchRequest,
  PolicySearchResult,
  ProjectSkill,
  ProjectSkillReview,
  REVIEW_LEVELS,
  ReviewLevel,
  ReviewLevelModels,
} from '../data-access/policy.models';
import { toReviewLevelModels } from './project-review';
import { PolicyService } from '../data-access/policy.service';
import { SkillListItem } from '../../skills/data-access/skill.models';
import { SkillService } from '../../skills/data-access/skill.service';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { MarkdownContentComponent } from '../../skills/ui/markdown-content.component';
import { policyHistoryLink } from './policy-history';
import { policySearchRequests } from './policy-search-requests';

interface PolicyListView {
  readonly scope: PolicyScope;
  readonly title: string;
  readonly subtitle: string;
  readonly backLink: string | null;
  readonly backLabel: string | null;
  readonly addRoute: readonly string[];
  readonly topicNames: readonly string[];
  readonly repositoryPaths: readonly string[];
  readonly skills: readonly ProjectSkill[];
  readonly reviewLevelModels: ReviewLevelModels;
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
    }
  | {
      readonly kind: 'skill';
      readonly projectId: string;
      readonly value: string;
    }
  | {
      readonly kind: 'remove-skill';
      readonly projectId: string;
      readonly value: string;
    }
  | {
      readonly kind: 'update-skill';
      readonly projectId: string;
      readonly skillId: string;
      readonly review: ProjectSkillReview;
    }
  | {
      readonly kind: 'review-models';
      readonly projectId: string;
      readonly levelModels: ReviewLevelModels;
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
  imports: [
    AsyncPipe,
    FormsModule,
    MarkdownContentComponent,
    PaginationComponent,
    RouterLink,
  ],
  templateUrl: './policy-list.page.html',
  styleUrls: ['./policy-list.page.css', '../ui/knowledge-list.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PolicyListPage {
  private readonly route = inject(ActivatedRoute);
  private readonly policies = inject(PolicyService);
  private readonly skillCatalog = inject(SkillService);
  private readonly searchRequests = new Subject<string>();
  private readonly pageRequests = new Subject<number>();
  private readonly request$ = policySearchRequests(
    this.searchRequests,
    this.pageRequests,
  );
  private readonly actions = new Subject<PolicyAction>();
  private readonly connectionRequests = new Subject<ProjectConnectionAction>();

  protected readonly emptyBlocks = [];
  protected readonly editingPolicyId = signal<string | null>(null);
  protected readonly confirmingPolicyId = signal<string | null>(null);
  protected readonly confirmingProjectTopicName = signal<string | null>(null);
  protected readonly confirmingProjectSkillId = signal<string | null>(null);
  protected readonly skillNameError = signal<string | null>(null);
  protected readonly reviewLevels = REVIEW_LEVELS;
  protected readonly editingSkillReviewId = signal<string | null>(null);
  protected readonly editingReviewModels = signal(false);
  protected readonly reviewModelsError = signal<string | null>(null);
  protected readonly searchText = signal('');

  private readonly scope$ = combineLatest([
    this.route.data,
    this.route.paramMap,
  ]).pipe(
    map(([data, params]) => policyScopeFromRoute(data['policyScope'], params)),
    distinctUntilChanged(
      (previous, current) => JSON.stringify(previous) === JSON.stringify(current),
    ),
  );

  /** Skills offered for linking; loaded only on a project page. */
  private readonly skillOptions$: Observable<readonly SkillListItem[]> =
    this.scope$.pipe(
      switchMap(scope =>
        scope?.kind === 'project'
          ? this.skillCatalog
              .search({
                page: 1,
                pageSize: 100,
                search: '',
                tag: '',
                hasReferences: null,
                hasAttachments: null,
                sortBy: 'Name',
                sortDirection: 'Ascending',
              })
              .pipe(
                map(result => result.items),
                catchError(() => of([])),
              )
          : of([]),
      ),
      startWith([]),
    );

  private readonly state$: Observable<LoadState<PolicyListView>> =
    combineLatest({ scope: this.scope$, request: this.request$ }).pipe(
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

  private readonly connectionMutation$: Observable<ProjectConnectionState> =
    this.connectionRequests.pipe(
      exhaustMap(action => {
        const request$ = this.executeConnectionAction(action);

        return request$.pipe(
          tap(() => {
            if (action.kind === 'remove-topic') {
              this.confirmingProjectTopicName.set(null);
            }
            if (action.kind === 'remove-skill') {
              this.confirmingProjectSkillId.set(null);
            }
            if (action.kind === 'update-skill') {
              this.editingSkillReviewId.set(null);
            }
            if (action.kind === 'review-models') {
              this.editingReviewModels.set(false);
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
    mutation: this.mutation$,
    connectionMutation: this.connectionMutation$,
    skillOptions: this.skillOptions$,
  }).pipe(shareReplay({ bufferSize: 1, refCount: true }));

  protected historyLink(
    scope: PolicyScope,
    policyId: string,
  ): readonly string[] {
    return policyHistoryLink(scope, policyId);
  }

  protected search(search: string): void {
    this.searchText.set(search);
    this.searchRequests.next(search);
  }

  protected goToPage(page: number): void {
    this.pageRequests.next(page);
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

  protected addProjectSkill(
    projectId: string,
    skillName: string,
    skillOptions: readonly SkillListItem[],
  ): void {
    const name = skillName.trim().toLowerCase();
    if (!name) return;

    const skill = skillOptions.find(option => option.name.toLowerCase() === name);
    if (!skill) {
      this.skillNameError.set(`No skill is named "${skillName.trim()}".`);
      return;
    }

    this.skillNameError.set(null);
    this.connectionRequests.next({ kind: 'skill', projectId, value: skill.id });
  }

  protected startRemovingProjectSkill(skillId: string): void {
    this.confirmingProjectSkillId.set(skillId);
  }

  protected cancelRemovingProjectSkill(): void {
    this.confirmingProjectSkillId.set(null);
  }

  protected removeProjectSkill(projectId: string, skillId: string): void {
    this.connectionRequests.next({
      kind: 'remove-skill',
      projectId,
      value: skillId,
    });
  }

  protected startEditingSkillReview(skillId: string): void {
    this.editingSkillReviewId.set(skillId);
  }

  protected cancelEditingSkillReview(): void {
    this.editingSkillReviewId.set(null);
  }

  protected saveSkillReview(
    projectId: string,
    skillId: string,
    form: {
      readonly usedForReview?: boolean;
      readonly reviewLevel?: ReviewLevel | '';
      readonly reviewNotes?: string;
    },
  ): void {
    this.connectionRequests.next({
      kind: 'update-skill',
      projectId,
      skillId,
      review: {
        usedForReview: !!form.usedForReview,
        reviewLevel: form.reviewLevel || null,
        reviewNotes: (form.reviewNotes ?? '').trim(),
      },
    });
  }

  protected startEditingReviewModels(): void {
    this.reviewModelsError.set(null);
    this.editingReviewModels.set(true);
  }

  protected cancelEditingReviewModels(): void {
    this.reviewModelsError.set(null);
    this.editingReviewModels.set(false);
  }

  protected saveReviewModels(
    projectId: string,
    form: Readonly<Record<string, string | undefined>>,
  ): void {
    const result = toReviewLevelModels(form);
    if (result.status === 'invalid') {
      this.reviewModelsError.set(result.message);
      return;
    }

    this.reviewModelsError.set(null);
    this.connectionRequests.next({
      kind: 'review-models',
      projectId,
      levelModels: result.levelModels,
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
      case 'skill':
        return this.policies.addProjectSkill(action.projectId, action.value);
      case 'remove-skill':
        return this.policies.removeProjectSkill(
          action.projectId,
          action.value,
        );
      case 'update-skill':
        return this.policies.updateProjectSkill(
          action.projectId,
          action.skillId,
          action.review,
        );
      case 'review-models':
        return this.policies.setProjectReviewLevelModels(
          action.projectId,
          action.levelModels,
        );
    }
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
          addRoute: ['/policies', 'general', 'new'],
          topicNames: [],
          repositoryPaths: [],
          skills: [],
          reviewLevelModels: {},
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
          skills: [],
          reviewLevelModels: {},
          result,
        })),
      );
    }

    if (scope.kind === 'agentFamily') {
      return policies$.pipe(
        map(result => ({
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
          skills: [],
          reviewLevelModels: {},
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
      skills: project.skills,
      reviewLevelModels: project.reviewLevelModels,
      result,
    };
  }
}
