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
  map,
  of,
  shareReplay,
  startWith,
  switchMap,
} from 'rxjs';
import { LoadState, toUserMessage } from '../../../core/http/load-state';
import { PagedResult } from '../../../core/store/entity-store.service';
import { PolicySearchRequest } from '../data-access/policy.models';
import { PolicyService } from '../data-access/policy.service';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { policySearchRequests } from './policy-search-requests';

type DirectoryKind = 'topics' | 'projects';

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
  readonly addRoute: readonly string[];
  readonly result: PagedResult<DirectoryEntry>;
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
  protected readonly searchText = signal('');

  private readonly kind$ = this.route.data.pipe(
    map(data => (data['directoryKind'] === 'projects' ? 'projects' : 'topics')),
    distinctUntilChanged(),
  );

  protected readonly state$: Observable<LoadState<DirectoryListView>> =
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

  protected search(search: string): void {
    this.searchText.set(search);
    this.searchRequests.next(search);
  }

  protected goToPage(page: number): void {
    this.pageRequests.next(page);
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
          addRoute: ['/policies', 'topics', 'new'],
          result: {
            ...result,
            items: result.items.map(topic => ({
              id: topic.id,
              name: topic.name,
              description: topic.description,
              metadata: `${topic.policyCount} ${topic.policyCount === 1 ? 'policy' : 'policies'}`,
              route: ['/policies', 'topics', topic.name],
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
