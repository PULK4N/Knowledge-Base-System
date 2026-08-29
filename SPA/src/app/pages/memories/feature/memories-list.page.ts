import { AsyncPipe, DatePipe } from '@angular/common';
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
import { PagedResult } from '../../../core/store/entity-store.service';
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
  MemorySearchRequest,
  MemorySearchSortField,
  MemorySummary,
} from '../data-access/memory.models';
import { MemoryService } from '../data-access/memory.service';
import {
  MEMORY_PAGE_SIZES,
  equalMemorySearchRequest,
  memorySearchQueryParams,
  parseMemorySearchRequest,
} from './memory-list-state';
import { memoryTitle } from './memory-title';

const MEMORY_SORT_OPTIONS: readonly ListControlOption[] = [
  { value: 'LastActivity', label: 'Last activity' },
  { value: 'PromptCount', label: 'Prompt count' },
  { value: 'FirstPrompt', label: 'First prompt' },
  { value: 'LastPrompt', label: 'Last prompt' },
  { value: 'SummaryUpdated', label: 'Summary updated' },
];
const RELEVANCE_SORT_OPTION: ListControlOption = {
  value: 'Relevance',
  label: 'Relevance',
};
const SUMMARY_FILTER_OPTIONS: readonly ListControlOption[] = [
  { value: '', label: 'Any' },
  { value: 'true', label: 'Has summary' },
  { value: 'false', label: 'No summary' },
];

interface MemoryListItem extends MemorySummary {
  readonly title: string;
}

@Component({
  selector: 'app-memories-list-page',
  imports: [
    AsyncPipe,
    DatePipe,
    ListControlsComponent,
    ListFiltersComponent,
    PaginationComponent,
    RouterLink,
  ],
  templateUrl: './memories-list.page.html',
  styleUrl: './memories-list.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MemoriesListPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly memories = inject(MemoryService);

  protected readonly pageSizes = MEMORY_PAGE_SIZES;

  private readonly request$ = this.route.queryParamMap.pipe(
    map(parseMemorySearchRequest),
    distinctUntilChanged(equalMemorySearchRequest),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  private readonly state$: Observable<
    LoadState<PagedResult<MemoryListItem>>
  > = this.request$.pipe(
    switchMap(request =>
      this.memories.search(request).pipe(
        map(result => ({
          status: 'success',
          data: {
            ...result,
            items: result.items.map(memory => ({
              ...memory,
              title: memoryTitle(memory.summary),
            })),
          },
        }) as const),
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

  protected sortOptions(
    request: MemorySearchRequest,
  ): readonly ListControlOption[] {
    return request.semanticSearch
      ? [RELEVANCE_SORT_OPTION, ...MEMORY_SORT_OPTIONS]
      : MEMORY_SORT_OPTIONS;
  }

  protected filters(request: MemorySearchRequest): readonly ListFilter[] {
    return [
      {
        kind: 'select',
        key: 'hasSummary',
        label: 'Summary',
        value: this.booleanFilterValue(request.hasSummary),
        options: SUMMARY_FILTER_OPTIONS,
      },
      {
        kind: 'text',
        type: 'number',
        key: 'minimumPromptCount',
        label: 'Minimum prompts',
        value: request.minimumPromptCount?.toString() ?? '',
        placeholder: 'Any',
        min: 1,
        step: 1,
      },
    ];
  }

  protected search(request: MemorySearchRequest, search: string): void {
    this.navigate(
      {
        ...request,
        page: 1,
        search,
        semanticSearch: '',
        sortBy:
          request.sortBy === 'Relevance'
            ? 'LastActivity'
            : request.sortBy,
      },
      true,
    );
  }

  protected semanticSearch(
    request: MemorySearchRequest,
    semanticSearch: string,
  ): void {
    const hasSemanticSearch = semanticSearch.trim().length > 0;
    const isEnteringSemanticMode =
      hasSemanticSearch && !request.semanticSearch;
    const sortBy = !hasSemanticSearch
      ? 'LastActivity'
      : isEnteringSemanticMode
        ? 'Relevance'
        : request.sortBy;
    const sortDirection =
      !hasSemanticSearch || isEnteringSemanticMode
        ? 'Descending'
        : request.sortDirection;
    this.navigate(
      {
        ...request,
        page: 1,
        search: '',
        semanticSearch,
        sortBy,
        sortDirection,
      },
      true,
    );
  }

  protected filter(
    request: MemorySearchRequest,
    change: ListFilterChange,
  ): void {
    if (change.key === 'hasSummary') {
      this.navigate({
        ...request,
        page: 1,
        hasSummary: this.readBooleanFilterValue(change.value),
      });
      return;
    }

    if (change.key === 'minimumPromptCount') {
      this.navigate(
        {
          ...request,
          page: 1,
          minimumPromptCount: this.readMinimumPromptCount(change.value),
        },
        true,
      );
    }
  }

  protected sort(request: MemorySearchRequest, sortBy: string): void {
    if (!this.sortOptions(request).some(option => option.value === sortBy)) {
      return;
    }

    this.navigate({
      ...request,
      page: 1,
      sortBy: sortBy as MemorySearchSortField,
    });
  }

  protected changeDirection(
    request: MemorySearchRequest,
    sortDirection: ListSortDirection,
  ): void {
    this.navigate({ ...request, page: 1, sortDirection });
  }

  protected changePageSize(
    request: MemorySearchRequest,
    pageSize: number,
  ): void {
    this.navigate({ ...request, page: 1, pageSize });
  }

  protected goToPage(request: MemorySearchRequest, page: number): void {
    this.navigate({ ...request, page });
  }

  private booleanFilterValue(value: boolean | null): string {
    return value === null ? '' : String(value);
  }

  private readBooleanFilterValue(value: string): boolean | null {
    return value === 'true' ? true : value === 'false' ? false : null;
  }

  private readMinimumPromptCount(value: string): number | null {
    if (value.trim() === '') return null;

    const parsed = Number(value);
    return Number.isSafeInteger(parsed) && parsed >= 1 ? parsed : null;
  }

  private navigate(request: MemorySearchRequest, replaceUrl = false): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: memorySearchQueryParams(request),
      replaceUrl,
    });
  }
}
