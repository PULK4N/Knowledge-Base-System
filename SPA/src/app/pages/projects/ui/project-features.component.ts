import { AsyncPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
} from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import {
  Observable,
  Subject,
  catchError,
  combineLatest,
  map,
  of,
  shareReplay,
  startWith,
  switchMap,
} from 'rxjs';
import { LoadState, toUserMessage } from '../../../core/http/load-state';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { FeatureSearchResult } from '../../features/data-access/feature.models';
import { FeatureService } from '../../features/data-access/feature.service';
import { policySearchRequests } from '../../policies/feature/policy-search-requests';

/** The features that belong to one project, newest change first. */
@Component({
  selector: 'app-project-features',
  imports: [AsyncPipe, PaginationComponent, RouterLink],
  templateUrl: './project-features.component.html',
  styleUrl: './project-pages.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectFeaturesComponent {
  private readonly features = inject(FeatureService);
  private readonly searchRequests = new Subject<string>();
  private readonly pageRequests = new Subject<number>();

  readonly projectId = input.required<string>();

  protected readonly state$: Observable<LoadState<FeatureSearchResult>> =
    combineLatest({
      projectId: toObservable(this.projectId),
      request: policySearchRequests(this.searchRequests, this.pageRequests),
    }).pipe(
      switchMap(({ projectId, request }) =>
        this.features
          .search({
            page: request.page,
            pageSize: 10,
            search: request.search,
            projectId,
            sortBy: 'UpdatedAt',
            sortDirection: 'Descending',
          })
          .pipe(
            map(data => ({ status: 'success', data }) as const),
            startWith({ status: 'loading' } as const),
            catchError(error =>
              of({ status: 'error', message: toUserMessage(error) } as const),
            ),
          ),
      ),
      shareReplay({ bufferSize: 1, refCount: true }),
    );

  protected search(search: string): void {
    this.searchRequests.next(search);
  }

  protected goToPage(page: number): void {
    this.pageRequests.next(page);
  }
}
