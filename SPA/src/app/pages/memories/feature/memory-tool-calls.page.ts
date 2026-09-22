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
import { ListSortDirection } from '../../../shared/list-state/list-state';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import {
  MemoryToolCallSearchItem,
  MemoryToolCallSearchRequest,
  MemoryToolCallSearchSortField,
} from '../data-access/memory.models';
import { MemoryService } from '../data-access/memory.service';
import { MemoriesTabsComponent } from '../ui/memories-tabs.component';
import {
  MEMORY_TOOL_CALL_PAGE_SIZES,
  equalMemoryToolCallSearchRequest,
  memoryToolCallSearchQueryParams,
  parseMemoryToolCallSearchRequest,
} from './memory-tool-call-list-state';

const TOOL_CALL_SORT_OPTIONS: readonly ListControlOption[] = [
  { value: 'Timestamp', label: 'Executed at' },
  { value: 'ToolName', label: 'Tool name' },
];

@Component({
  selector: 'app-memory-tool-calls-page',
  imports: [
    AsyncPipe,
    DatePipe,
    ListControlsComponent,
    MemoriesTabsComponent,
    PaginationComponent,
    RouterLink,
  ],
  templateUrl: './memory-tool-calls.page.html',
  styleUrl: './memory-tool-calls.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MemoryToolCallsPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly memories = inject(MemoryService);

  protected readonly pageSizes = MEMORY_TOOL_CALL_PAGE_SIZES;
  protected readonly sortOptions = TOOL_CALL_SORT_OPTIONS;

  private readonly request$ = this.route.queryParamMap.pipe(
    map(parseMemoryToolCallSearchRequest),
    distinctUntilChanged(equalMemoryToolCallSearchRequest),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  private readonly state$: Observable<
    LoadState<PagedResult<MemoryToolCallSearchItem>>
  > = this.request$.pipe(
    switchMap(request =>
      this.memories.searchToolCalls(request).pipe(
        map(result => ({ status: 'success', data: result }) as const),
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

  protected search(
    request: MemoryToolCallSearchRequest,
    search: string,
  ): void {
    this.navigate({ ...request, page: 1, search }, true);
  }

  protected filterByToolName(
    request: MemoryToolCallSearchRequest,
    toolName: string,
  ): void {
    this.navigate({ ...request, page: 1, toolName }, true);
  }

  protected sort(
    request: MemoryToolCallSearchRequest,
    sortBy: string,
  ): void {
    if (!this.sortOptions.some(option => option.value === sortBy)) return;

    this.navigate({
      ...request,
      page: 1,
      sortBy: sortBy as MemoryToolCallSearchSortField,
    });
  }

  protected changeDirection(
    request: MemoryToolCallSearchRequest,
    sortDirection: ListSortDirection,
  ): void {
    this.navigate({ ...request, page: 1, sortDirection });
  }

  protected changePageSize(
    request: MemoryToolCallSearchRequest,
    pageSize: number,
  ): void {
    this.navigate({ ...request, page: 1, pageSize });
  }

  protected goToPage(
    request: MemoryToolCallSearchRequest,
    page: number,
  ): void {
    this.navigate({ ...request, page });
  }

  private navigate(
    request: MemoryToolCallSearchRequest,
    replaceUrl = false,
  ): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: memoryToolCallSearchQueryParams(request),
      replaceUrl,
    });
  }
}
