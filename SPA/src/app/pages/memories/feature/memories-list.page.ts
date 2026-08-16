import { AsyncPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import {
  BehaviorSubject,
  Observable,
  catchError,
  debounceTime,
  distinctUntilChanged,
  map,
  of,
  shareReplay,
  startWith,
  switchMap,
} from 'rxjs';
import { LoadState, toUserMessage } from '../../../core/http/load-state';
import { PagedResult } from '../../../core/store/entity-store.service';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import {
  MemorySearchRequest,
  MemorySummary,
} from '../data-access/memory.models';
import { MemoryService } from '../data-access/memory.service';
import { memoryTitle } from './memory-title';

const PAGE_SIZE = 5;

interface MemoryListItem extends MemorySummary {
  readonly title: string;
}

@Component({
  selector: 'app-memories-list-page',
  imports: [AsyncPipe, DatePipe, PaginationComponent],
  templateUrl: './memories-list.page.html',
  styleUrl: './memories-list.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MemoriesListPage {
  private readonly memories = inject(MemoryService);
  private readonly querySubject = new BehaviorSubject<MemorySearchRequest>({
    page: 1,
    pageSize: PAGE_SIZE,
    search: '',
  });

  protected readonly state$: Observable<
    LoadState<PagedResult<MemoryListItem>>
  > = this.querySubject.pipe(
    debounceTime(200),
    distinctUntilChanged(
      (previous, current) =>
        previous.page === current.page &&
        previous.search.trim() === current.search.trim(),
    ),
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

  protected search(search: string): void {
    this.querySubject.next({
      ...this.querySubject.value,
      page: 1,
      search,
    });
  }

  protected goToPage(page: number): void {
    this.querySubject.next({ ...this.querySubject.value, page });
  }
}
