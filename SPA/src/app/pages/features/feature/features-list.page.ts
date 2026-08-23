import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  Observable,
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
import {
  ListControlOption,
  ListControlsComponent,
} from '../../../shared/list-controls/list-controls.component';
import { ListSortDirection } from '../../../shared/list-state/list-state';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { PolicyService } from '../../policies/data-access/policy.service';
import { ShortIdPipe } from '../../skills/ui/short-id.pipe';
import {
  FeatureSearchRequest,
  FeatureSearchResult,
  FeatureSearchSortField,
} from '../data-access/feature.models';
import { FeatureService } from '../data-access/feature.service';
import {
  FEATURE_PAGE_SIZES,
  equalFeatureSearchRequest,
  featureSearchQueryParams,
  parseFeatureSearchRequest,
} from './feature-list-state';

const FEATURE_SORT_OPTIONS: readonly ListControlOption[] = [
  { value: 'Name', label: 'Name' },
  { value: 'PlanCount', label: 'Plan count' },
  { value: 'RecordCount', label: 'Record count' },
];
const ALL_PROJECTS_OPTION: ListControlOption = {
  value: '',
  label: 'All projects',
};

type ProjectFilterState = LoadState<readonly ListControlOption[]>;

@Component({
  selector: 'app-features-list-page',
  imports: [
    AsyncPipe,
    ListControlsComponent,
    PaginationComponent,
    RouterLink,
    ShortIdPipe,
  ],
  templateUrl: './features-list.page.html',
  styleUrls: ['./features-list.page.css', '../ui/feature-pages.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FeaturesListPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly features = inject(FeatureService);
  private readonly policies = inject(PolicyService);

  protected readonly sortOptions = FEATURE_SORT_OPTIONS;
  protected readonly pageSizes = FEATURE_PAGE_SIZES;

  private readonly request$ = this.route.queryParamMap.pipe(
    map(parseFeatureSearchRequest),
    distinctUntilChanged(equalFeatureSearchRequest),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  private readonly state$: Observable<LoadState<FeatureSearchResult>> =
    this.request$.pipe(
      switchMap(request =>
        this.features.search(request).pipe(
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

  private readonly projectFilters$: Observable<ProjectFilterState> =
    this.policies.searchProjects({ page: 1, pageSize: 100, search: '' }).pipe(
      map(result =>
        ({
          status: 'success',
          data: [
            ALL_PROJECTS_OPTION,
            ...result.items.map(project => ({
              value: project.id,
              label: project.name,
            })),
          ],
        }) as const,
      ),
      startWith({ status: 'loading' } as const),
      catchError(error =>
        of({ status: 'error', message: toUserMessage(error) } as const),
      ),
      shareReplay({ bufferSize: 1, refCount: true }),
    );

  protected readonly vm$ = combineLatest({
    state: this.state$,
    request: this.request$,
    projects: this.projectFilters$,
  }).pipe(shareReplay({ bufferSize: 1, refCount: true }));

  protected search(request: FeatureSearchRequest, search: string): void {
    this.navigate({ ...request, page: 1, search }, true);
  }

  protected filterByProject(
    request: FeatureSearchRequest,
    projectId: string,
  ): void {
    this.navigate({ ...request, page: 1, projectId });
  }

  protected sort(
    request: FeatureSearchRequest,
    sortBy: string,
  ): void {
    if (!FEATURE_SORT_OPTIONS.some(option => option.value === sortBy)) return;

    this.navigate({
      ...request,
      page: 1,
      sortBy: sortBy as FeatureSearchSortField,
    });
  }

  protected changeDirection(
    request: FeatureSearchRequest,
    sortDirection: ListSortDirection,
  ): void {
    this.navigate({ ...request, page: 1, sortDirection });
  }

  protected changePageSize(
    request: FeatureSearchRequest,
    pageSize: number,
  ): void {
    this.navigate({ ...request, page: 1, pageSize });
  }

  protected goToPage(request: FeatureSearchRequest, page: number): void {
    this.navigate({ ...request, page });
  }

  protected projectOptions(
    state: ProjectFilterState,
    selectedProjectId: string,
  ): readonly ListControlOption[] {
    const options =
      state.status === 'success' ? state.data : [ALL_PROJECTS_OPTION];
    if (
      !selectedProjectId ||
      options.some(option => option.value === selectedProjectId)
    ) {
      return options;
    }

    return [
      ...options,
      { value: selectedProjectId, label: `Project ${selectedProjectId}` },
    ];
  }

  private navigate(request: FeatureSearchRequest, replaceUrl = false): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: featureSearchQueryParams(request),
      replaceUrl,
    });
  }
}
