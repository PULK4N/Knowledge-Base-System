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
import {
  ListFilter,
  ListFilterChange,
  ListFiltersComponent,
} from '../../../shared/list-filters/list-filters.component';
import { ListSortDirection } from '../../../shared/list-state/list-state';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import {
  SkillSearchRequest,
  SkillSearchResult,
  SkillSearchSortField,
} from '../data-access/skill.models';
import { SkillService } from '../data-access/skill.service';
import { ShortIdPipe } from '../ui/short-id.pipe';
import {
  SKILL_PAGE_SIZES,
  equalSkillSearchRequest,
  parseSkillSearchRequest,
  skillSearchQueryParams,
} from './skill-list-state';

const SKILL_SORT_OPTIONS: readonly ListControlOption[] = [
  { value: 'Name', label: 'Name' },
  { value: 'ReferenceCount', label: 'Reference count' },
  { value: 'AttachmentCount', label: 'Attachment count' },
];
const PRESENCE_FILTER_OPTIONS: readonly ListControlOption[] = [
  { value: '', label: 'Any' },
  { value: 'true', label: 'Has at least one' },
  { value: 'false', label: 'Has none' },
];

@Component({
  selector: 'app-skills-list-page',
  imports: [
    AsyncPipe,
    ListControlsComponent,
    ListFiltersComponent,
    PaginationComponent,
    RouterLink,
    ShortIdPipe,
  ],
  templateUrl: './skills-list.page.html',
  styleUrl: './skills-list.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkillsListPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly skills = inject(SkillService);

  protected readonly sortOptions = SKILL_SORT_OPTIONS;
  protected readonly pageSizes = SKILL_PAGE_SIZES;

  private readonly request$ = this.route.queryParamMap.pipe(
    map(parseSkillSearchRequest),
    distinctUntilChanged(equalSkillSearchRequest),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  private readonly state$: Observable<LoadState<SkillSearchResult>> =
    this.request$.pipe(
      switchMap(request =>
        this.skills.search(request).pipe(
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

  protected readonly vm$ = combineLatest({
    request: this.request$,
    state: this.state$,
  }).pipe(shareReplay({ bufferSize: 1, refCount: true }));

  protected filters(request: SkillSearchRequest): readonly ListFilter[] {
    return [
      {
        kind: 'text',
        key: 'tag',
        label: 'Exact tag',
        value: request.tag,
        placeholder: 'For example angular',
        maxLength: 500,
      },
      {
        kind: 'select',
        key: 'hasReferences',
        label: 'References',
        value: this.booleanFilterValue(request.hasReferences),
        options: PRESENCE_FILTER_OPTIONS,
      },
      {
        kind: 'select',
        key: 'hasAttachments',
        label: 'Attachments',
        value: this.booleanFilterValue(request.hasAttachments),
        options: PRESENCE_FILTER_OPTIONS,
      },
    ];
  }

  protected search(request: SkillSearchRequest, search: string): void {
    this.navigate({ ...request, page: 1, search }, true);
  }

  protected filter(
    request: SkillSearchRequest,
    change: ListFilterChange,
  ): void {
    if (change.key === 'tag') {
      this.navigate({ ...request, page: 1, tag: change.value }, true);
      return;
    }

    const value = this.readBooleanFilterValue(change.value);
    if (change.key === 'hasReferences') {
      this.navigate({ ...request, page: 1, hasReferences: value });
    } else if (change.key === 'hasAttachments') {
      this.navigate({ ...request, page: 1, hasAttachments: value });
    }
  }

  protected sort(request: SkillSearchRequest, sortBy: string): void {
    if (!SKILL_SORT_OPTIONS.some(option => option.value === sortBy)) return;

    this.navigate({
      ...request,
      page: 1,
      sortBy: sortBy as SkillSearchSortField,
    });
  }

  protected changeDirection(
    request: SkillSearchRequest,
    sortDirection: ListSortDirection,
  ): void {
    this.navigate({ ...request, page: 1, sortDirection });
  }

  protected changePageSize(
    request: SkillSearchRequest,
    pageSize: number,
  ): void {
    this.navigate({ ...request, page: 1, pageSize });
  }

  protected goToPage(request: SkillSearchRequest, page: number): void {
    this.navigate({ ...request, page });
  }

  private booleanFilterValue(value: boolean | null): string {
    return value === null ? '' : String(value);
  }

  private readBooleanFilterValue(value: string): boolean | null {
    return value === 'true' ? true : value === 'false' ? false : null;
  }

  private navigate(request: SkillSearchRequest, replaceUrl = false): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: skillSearchQueryParams(request),
      replaceUrl,
    });
  }
}
