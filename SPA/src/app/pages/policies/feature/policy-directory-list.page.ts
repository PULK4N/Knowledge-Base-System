import { AsyncPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
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
import { PagedResult } from '../../../core/store/entity-store.service';
import { PolicySearchRequest } from '../data-access/policy.models';
import { PolicyService } from '../data-access/policy.service';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import {
  DirectoryKind,
  directoryKindFromRoute,
} from './policy-directory-kind';
import { policySearchRequests } from './policy-search-requests';


interface DirectoryEntry {
  readonly id: string;
  readonly name: string;
  readonly description: string;
  readonly metadata: string;
  readonly route: readonly string[];
}

interface DirectoryListView {
  readonly kind: DirectoryKind;
  readonly title: string;
  readonly subtitle: string;
  readonly singularLabel: string;
  readonly pluralLabel: string;
  readonly entryHeading: string;
  readonly metadataHeading: string;
  readonly icon: string;
  readonly addRoute: readonly string[];
  readonly result: PagedResult<DirectoryEntry>;
}

type TopicRemovalState =
  | { readonly status: 'idle' }
  | { readonly status: 'deleting'; readonly topicName: string }
  | {
      readonly status: 'error';
      readonly topicName: string;
      readonly message: string;
    };


function policyCountLabel(policyCount: number): string {
  return `${policyCount} ${policyCount === 1 ? 'policy' : 'policies'}`;
}

@Component({
  selector: 'app-policy-directory-list-page',
  imports: [AsyncPipe, PaginationComponent, RouterLink],
  templateUrl: './policy-directory-list.page.html',
  styleUrls: [
    './policy-directory-list.page.css',
    '../ui/knowledge-list.css',
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PolicyDirectoryListPage {
  private readonly route = inject(ActivatedRoute);
  private readonly policies = inject(PolicyService);
  private readonly searchRequests = new Subject<string>();
  private readonly pageRequests = new Subject<number>();
  private readonly request$ = policySearchRequests(
    this.searchRequests,
    this.pageRequests,
  );
  private readonly topicRemovalRequests = new Subject<string>();
  protected readonly searchText = signal('');
  protected readonly confirmingTopicName = signal<string | null>(null);

  private readonly kind$ = this.route.data.pipe(
    map(data => directoryKindFromRoute(data['directoryKind'])),
    distinctUntilChanged(),
  );

  private readonly state$: Observable<LoadState<DirectoryListView>> =
    combineLatest({ kind: this.kind$, request: this.request$ }).pipe(
      distinctUntilChanged(
        (previous, current) =>
          previous.kind === current.kind &&
          previous.request.page === current.request.page &&
          previous.request.search.trim() === current.request.search.trim(),
      ),
      switchMap(({ kind, request }) =>
        this.load(kind, request).pipe(
          map(data => ({ status: 'success', data }) as const),
          startWith({ status: 'loading' } as const),
          catchError(error =>
            of({
              status: 'error',
              message: toUserMessage(error),
            } as const),
          ),
        ),
      ),
      shareReplay({ bufferSize: 1, refCount: true }),
    );

  private readonly topicRemoval$: Observable<TopicRemovalState> =
    this.topicRemovalRequests.pipe(
      exhaustMap(topicName =>
        this.policies.removeTopic(topicName).pipe(
          tap(() => this.confirmingTopicName.set(null)),
          map(() => ({ status: 'idle' }) as const),
          startWith({ status: 'deleting', topicName } as const),
          catchError(error =>
            of({
              status: 'error',
              topicName,
              message: toUserMessage(error),
            } as const),
          ),
        ),
      ),
      startWith({ status: 'idle' } as const),
    );

  protected readonly vm$ = combineLatest({
    state: this.state$,
    topicRemoval: this.topicRemoval$,
  }).pipe(shareReplay({ bufferSize: 1, refCount: true }));

  protected search(search: string): void {
    this.searchText.set(search);
    this.searchRequests.next(search);
  }

  protected goToPage(page: number): void {
    this.pageRequests.next(page);
  }

  protected startRemovingTopic(topicName: string): void {
    this.confirmingTopicName.set(topicName);
  }

  protected cancelRemovingTopic(): void {
    this.confirmingTopicName.set(null);
  }

  protected removeTopic(topicName: string): void {
    this.topicRemovalRequests.next(topicName);
  }

  private load(
    kind: DirectoryKind,
    request: PolicySearchRequest,
  ): Observable<DirectoryListView> {
    if (kind === 'topics') {
      return this.policies.searchTopics(request).pipe(
        map(result => ({
          kind,
          title: 'Topics',
          subtitle: 'Policy groups shared across projects',
          singularLabel: 'topic',
          pluralLabel: 'topics',
          entryHeading: 'Topic',
          metadataHeading: 'Policies',
          icon: '#',
          addRoute: ['/policies', 'topics', 'new'],
          result: {
            ...result,
            items: result.items.map(topic => ({
              id: topic.id,
              name: topic.name,
              description: topic.description,
              metadata: policyCountLabel(topic.policyCount),
              route: ['/policies', 'topics', topic.name],
            })),
          },
        })),
      );
    }

    if (kind === 'agent-families') {
      return this.policies.searchAgentFamilies(request).pipe(
        map(result => ({
          kind,
          title: 'Agent families',
          subtitle: 'Policies applied only to one kind of agent',
          singularLabel: 'agent family',
          pluralLabel: 'agent families',
          entryHeading: 'Agent family',
          metadataHeading: 'Policies',
          icon: '◆',
          addRoute: ['/policies', 'agent-families', 'new'],
          result: {
            ...result,
            items: result.items.map(agentFamily => ({
              id: agentFamily.id,
              name: agentFamily.name,
              description: agentFamily.description,
              metadata: policyCountLabel(agentFamily.policyCount),
              route: ['/policies', 'agent-families', agentFamily.name],
            })),
          },
        })),
      );
    }

    return this.policies.searchProjects(request).pipe(
      map(result => ({
        kind,
        title: 'Projects',
        subtitle: 'Project-specific policies and topic relationships',
        singularLabel: 'project',
        pluralLabel: 'projects',
        entryHeading: 'Project',
        metadataHeading: 'Repositories',
        icon: '▤',
        addRoute: ['/policies', 'projects', 'new'],
        result: {
          ...result,
          items: result.items.map(project => ({
            id: project.id,
            name: project.name,
            description:
              project.repositoryPaths[0] ?? 'No repositories connected',
            metadata: `${project.repositoryPaths.length} ${project.repositoryPaths.length === 1 ? 'repository' : 'repositories'}`,
            route: ['/policies', 'projects', project.id],
          })),
        },
      })),
    );
  }
}
