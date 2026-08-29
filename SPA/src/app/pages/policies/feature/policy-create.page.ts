import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
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
  tap,
} from 'rxjs';
import { toUserMessage } from '../../../core/http/load-state';
import { PolicyScope } from '../data-access/policy.models';
import { PolicyService } from '../data-access/policy.service';

interface PolicyCreateContext {
  readonly scope: PolicyScope;
  readonly title: string;
  readonly subtitle: string;
  readonly backLink: readonly string[];
}

interface CreatePolicyAction {
  readonly context: PolicyCreateContext;
  readonly title: string;
  readonly description: string;
}

type CreateState =
  | { readonly status: 'idle' }
  | { readonly status: 'saving' }
  | { readonly status: 'error'; readonly message: string };

function createContext(
  scopeKind: unknown,
  params: ParamMap,
): PolicyCreateContext | null {
  if (scopeKind === 'general') {
    return {
      scope: { kind: 'general' },
      title: 'Add general policy',
      subtitle: 'Create a rule that applies across projects and topics',
      backLink: ['/policies', 'general'],
    };
  }

  if (scopeKind === 'topic') {
    const topicName = params.get('topicName');
    return topicName
      ? {
          scope: { kind: 'topic', topicName },
          title: `Add policy to ${topicName}`,
          subtitle: 'Create a policy shared through this topic',
          backLink: ['/policies', 'topics', topicName],
        }
      : null;
  }

  if (scopeKind === 'project') {
    const projectId = params.get('projectId');
    return projectId
      ? {
          scope: { kind: 'project', projectId },
          title: 'Add project policy',
          subtitle: 'Create a policy that applies only to this project',
          backLink: ['/policies', 'projects', projectId],
        }
      : null;
  }

  return null;
}

@Component({
  selector: 'app-policy-create-page',
  imports: [AsyncPipe, FormsModule, RouterLink],
  templateUrl: './policy-create.page.html',
  styleUrl: '../../skills/ui/editor-form.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PolicyCreatePage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly policies = inject(PolicyService);
  private readonly createRequests = new Subject<CreatePolicyAction>();

  private readonly context$ = combineLatest([
    this.route.data,
    this.route.paramMap,
  ]).pipe(
    map(([data, params]) => createContext(data['policyScope'], params)),
    distinctUntilChanged(
      (previous, current) => JSON.stringify(previous) === JSON.stringify(current),
    ),
  );

  private readonly mutation$: Observable<CreateState> =
    this.createRequests.pipe(
      exhaustMap(action =>
        this.policies
          .addPolicy(action.context.scope, {
            title: action.title,
            description: action.description,
          })
          .pipe(
            tap(() => void this.router.navigate(action.context.backLink)),
            map(() => ({ status: 'idle' }) as const),
            startWith({ status: 'saving' } as const),
            catchError(error =>
              of({ status: 'error', message: toUserMessage(error) } as const),
            ),
          ),
      ),
      startWith({ status: 'idle' } as const),
    );

  protected readonly vm$ = combineLatest({
    context: this.context$,
    mutation: this.mutation$,
  }).pipe(shareReplay({ bufferSize: 1, refCount: true }));

  protected create(
    context: PolicyCreateContext,
    title: string,
    description: string,
  ): void {
    this.createRequests.next({
      context,
      title: title.trim(),
      description,
    });
  }
}
