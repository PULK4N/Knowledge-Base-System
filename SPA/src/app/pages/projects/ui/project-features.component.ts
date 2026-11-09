import { AsyncPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
  signal,
} from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import {
  Observable,
  catchError,
  combineLatest,
  map,
  of,
  shareReplay,
  startWith,
  switchMap,
} from 'rxjs';
import { LoadState, toUserMessage } from '../../../core/http/load-state';
import {
  ListControlOption,
  ListControlsComponent,
} from '../../../shared/list-controls/list-controls.component';
import { ListRowComponent } from '../../../shared/list-row/list-row.component';
import { ListSortDirection } from '../../../shared/list-state/list-state';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import {
  FeatureSearchResult,
  FeatureSearchSortField,
} from '../../features/data-access/feature.models';
import { FeatureService } from '../../features/data-access/feature.service';

const FEATURE_SORT_OPTIONS: readonly ListControlOption[] = [
  { value: 'UpdatedAt', label: 'Last updated' },
  { value: 'Name', label: 'Name' },
  { value: 'PlanCount', label: 'Plan count' },
  { value: 'RecordCount', label: 'Record count' },
];

interface ProjectFeatureListRequest {
  readonly page: number;
  readonly search: string;
  readonly sortBy: FeatureSearchSortField;
  readonly sortDirection: ListSortDirection;
}

/** The features that belong to one project, newest change first. */
@Component({
  selector: 'app-project-features',
  imports: [AsyncPipe, ListControlsComponent, ListRowComponent, PaginationComponent],
  templateUrl: './project-features.component.html',
  styleUrls: [
    '../../policies/feature/policy-directory-list.page.css',
    '../../policies/ui/knowledge-list.css',
    './project-pages.css',
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectFeaturesComponent {
  private readonly features = inject(FeatureService);

  readonly projectId = input.required<string>();

  protected readonly sortOptions = FEATURE_SORT_OPTIONS;
  protected readonly request = signal<ProjectFeatureListRequest>({
    page: 1,
    search: '',
    sortBy: 'UpdatedAt',
    sortDirection: 'Descending',
  });

  protected readonly state$: Observable<LoadState<FeatureSearchResult>> =
    combineLatest({
      projectId: toObservable(this.projectId),
      request: toObservable(this.request),
    }).pipe(
      switchMap(({ projectId, request }) =>
        this.features
          .search({ ...request, pageSize: 10, projectId })
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
    this.change({ page: 1, search: search.trim() });
  }

  protected sort(sortBy: string): void {
    if (!FEATURE_SORT_OPTIONS.some(option => option.value === sortBy)) return;

    this.change({ page: 1, sortBy: sortBy as FeatureSearchSortField });
  }

  protected changeDirection(sortDirection: ListSortDirection): void {
    this.change({ page: 1, sortDirection });
  }

  protected goToPage(page: number): void {
    this.change({ page });
  }

  private change(change: Partial<ProjectFeatureListRequest>): void {
    this.request.update(request => ({ ...request, ...change }));
  }
}
