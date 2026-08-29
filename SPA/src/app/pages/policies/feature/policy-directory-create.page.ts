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
  CreateProjectRequest,
  CreateTopicRequest,
} from '../data-access/policy.models';
import { PolicyService } from '../data-access/policy.service';

type DirectoryKind = 'topics' | 'projects';

type CreateDirectoryAction =
  | { readonly kind: 'topics'; readonly request: CreateTopicRequest }
  | { readonly kind: 'projects'; readonly request: CreateProjectRequest };

type CreateState =
  | { readonly status: 'idle' }
  | { readonly status: 'saving' }
  | { readonly status: 'error'; readonly message: string };

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

  protected readonly kind$ = this.route.data.pipe(
    map(
      (data): DirectoryKind =>
        data['directoryKind'] === 'projects' ? 'projects' : 'topics',
    ),
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
