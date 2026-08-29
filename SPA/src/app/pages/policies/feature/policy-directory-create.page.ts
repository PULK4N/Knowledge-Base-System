import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  Observable,
  Subject,
  catchError,
  distinctUntilChanged,
  exhaustMap,
  map,
  of,
  shareReplay,
  startWith,
  tap,
} from 'rxjs';
import { toUserMessage } from '../../../core/http/load-state';
import {
  CreateAgentFamilyRequest,
  CreateProjectRequest,
  CreateTopicRequest,
} from '../data-access/policy.models';
import { PolicyService } from '../data-access/policy.service';
import {
  DirectoryKind,
  directoryKindFromRoute,
} from './policy-directory-kind';

type CreateDirectoryAction =
  | { readonly kind: 'topics'; readonly request: CreateTopicRequest }
  | {
      readonly kind: 'agent-families';
      readonly request: CreateAgentFamilyRequest;
    }
  | { readonly kind: 'projects'; readonly request: CreateProjectRequest };

type CreateState =
  | { readonly status: 'idle' }
  | { readonly status: 'saving' }
  | { readonly status: 'error'; readonly message: string };

interface DirectoryCreateLabels {
  readonly kind: DirectoryKind;
  readonly entity: string;
  readonly singular: string;
  readonly plural: string;
  readonly subtitle: string;
  readonly backLink: string;
}

const DIRECTORY_CREATE_LABELS: Readonly<
  Record<DirectoryKind, DirectoryCreateLabels>
> = {
  topics: {
    kind: 'topics',
    entity: 'Topic',
    singular: 'topic',
    plural: 'topics',
    subtitle: 'Create a policy group that projects can reuse',
    backLink: '/policies/topics',
  },
  'agent-families': {
    kind: 'agent-families',
    entity: 'Agent family',
    singular: 'agent family',
    plural: 'agent families',
    subtitle: 'Create a policy group applied only to one kind of agent',
    backLink: '/policies/agent-families',
  },
  projects: {
    kind: 'projects',
    entity: 'Project',
    singular: 'project',
    plural: 'projects',
    subtitle: 'Create a project knowledge scope and connect its repositories',
    backLink: '/policies/projects',
  },
};

@Component({
  selector: 'app-policy-directory-create-page',
  imports: [AsyncPipe, FormsModule, RouterLink],
  templateUrl: './policy-directory-create.page.html',
  styleUrl: '../../skills/ui/editor-form.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PolicyDirectoryCreatePage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly policies = inject(PolicyService);
  private readonly createRequests = new Subject<CreateDirectoryAction>();

  protected readonly labels$ = this.route.data.pipe(
    map(data => DIRECTORY_CREATE_LABELS[
      directoryKindFromRoute(data['directoryKind'])
    ]),
    distinctUntilChanged(),
  );

  protected readonly state$: Observable<CreateState> = this.createRequests.pipe(
    exhaustMap(action => {
      if (action.kind === 'topics') {
        return this.policies.createTopic(action.request).pipe(
          tap(topic =>
            void this.router.navigate(['/policies', 'topics', topic.name]),
          ),
          map(() => ({ status: 'idle' }) as const),
          startWith({ status: 'saving' } as const),
          catchError(error =>
            of({ status: 'error', message: toUserMessage(error) } as const),
          ),
        );
      }

      if (action.kind === 'agent-families') {
        return this.policies.createAgentFamily(action.request).pipe(
          tap(agentFamily =>
            void this.router.navigate([
              '/policies',
              'agent-families',
              agentFamily.name,
            ]),
          ),
          map(() => ({ status: 'idle' }) as const),
          startWith({ status: 'saving' } as const),
          catchError(error =>
            of({ status: 'error', message: toUserMessage(error) } as const),
          ),
        );
      }

      return this.policies.createProject(action.request).pipe(
        tap(project =>
          void this.router.navigate(['/policies', 'projects', project.id]),
        ),
        map(() => ({ status: 'idle' }) as const),
        startWith({ status: 'saving' } as const),
        catchError(error =>
          of({ status: 'error', message: toUserMessage(error) } as const),
        ),
      );
    }),
    startWith({ status: 'idle' } as const),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  protected createTopic(topicName: string, description: string): void {
    this.createRequests.next({
      kind: 'topics',
      request: { topicName: topicName.trim(), description },
    });
  }

  protected createAgentFamily(
    agentFamilyName: string,
    description: string,
  ): void {
    this.createRequests.next({
      kind: 'agent-families',
      request: { agentFamilyName: agentFamilyName.trim(), description },
    });
  }

  protected createProject(
    projectName: string,
    projectDescription: string,
    repositoryPaths: string,
  ): void {
    this.createRequests.next({
      kind: 'projects',
      request: {
        projectName: projectName.trim(),
        projectDescription,
        repositoryPaths: [
          ...new Set(
            repositoryPaths
              .split('\n')
              .map(path => path.trim())
              .filter(Boolean),
          ),
        ],
      },
    });
  }
}
