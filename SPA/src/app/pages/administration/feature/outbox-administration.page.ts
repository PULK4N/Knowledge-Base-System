import { AsyncPipe, DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import {
  Observable,
  Subject,
  catchError,
  combineLatest,
  distinctUntilChanged,
  exhaustMap,
  filter,
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
  OutboxPayloadSearchRequest,
  OutboxPayloadSearchResult,
  OutboxPayloadSortField,
} from '../data-access/outbox-administration.models';
import { OutboxAdministrationService } from '../data-access/outbox-administration.service';
import {
  OUTBOX_PAGE_SIZES,
  OUTBOX_STATES,
  equalOutboxSearchRequest,
  outboxSearchQueryParams,
  parseOutboxSearchRequest,
} from './outbox-list-state';

const OUTBOX_SORT_OPTIONS: readonly ListControlOption[] = [
  { value: 'Id', label: 'Queue position' },
  { value: 'State', label: 'Delivery state' },
  { value: 'RetryCount', label: 'Retries' },
  { value: 'AggregateId', label: 'Aggregate' },
];
const COMPLETION_FILTER_OPTIONS: readonly ListControlOption[] = [
  { value: 'false', label: 'All payloads' },
  { value: 'true', label: 'Not completed successfully' },
];
const STATE_FILTER_OPTIONS: readonly ListControlOption[] = [
  { value: '', label: 'Any' },
  ...OUTBOX_STATES.map(state => ({ value: state, label: state })),
];

type RequeueAllState =
  | { readonly status: 'idle' }
  | { readonly status: 'requeuing' }
  | { readonly status: 'success'; readonly requeuedCount: number }
  | { readonly status: 'error'; readonly message: string };

type RequeueState =
  | { readonly status: 'idle' }
  | { readonly status: 'requeuing'; readonly outboxPayloadId: string }
  | { readonly status: 'success'; readonly outboxPayloadId: string }
  | {
      readonly status: 'error';
      readonly outboxPayloadId: string;
      readonly message: string;
    };

@Component({
  selector: 'app-outbox-administration-page',
  imports: [
    AsyncPipe,
    DatePipe,
    ListControlsComponent,
    ListFiltersComponent,
    PaginationComponent,
  ],
  templateUrl: './outbox-administration.page.html',
  styleUrl: './outbox-administration.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OutboxAdministrationPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly administration = inject(OutboxAdministrationService);
  private readonly requeueRequests = new Subject<string>();
  private readonly requeueAllRequests = new Subject<void>();

  protected readonly sortOptions = OUTBOX_SORT_OPTIONS;
  protected readonly pageSizes = OUTBOX_PAGE_SIZES;
  protected readonly expandedPayloadId = signal<string | null>(null);

  private readonly request$ = this.route.queryParamMap.pipe(
    map(parseOutboxSearchRequest),
    distinctUntilChanged(equalOutboxSearchRequest),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  private readonly requeueAll$: Observable<RequeueAllState> =
    this.requeueAllRequests.pipe(
      exhaustMap(() =>
        this.administration.requeueIncomplete().pipe(
          map(
            summary =>
              ({
                status: 'success',
                requeuedCount: summary.requeuedCount,
              }) as const,
          ),
          startWith({ status: 'requeuing' } as const),
          catchError(error =>
            of({
              status: 'error',
              message: toUserMessage(error),
            } as const),
          ),
        ),
      ),
      startWith({ status: 'idle' } as const),
      shareReplay({ bufferSize: 1, refCount: true }),
    );

  // A bulk requeue changes rows the list does not get back, so the current
  // search is run again after every successful one.
  private readonly reload$ = this.requeueAll$.pipe(
    filter(state => state.status === 'success'),
    startWith(undefined),
  );

  private readonly payloads$: Observable<
    LoadState<OutboxPayloadSearchResult>
  > = combineLatest([this.request$, this.reload$]).pipe(
    switchMap(([request]) =>
      this.administration.search(request).pipe(
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

  private readonly requeue$: Observable<RequeueState> =
    this.requeueRequests.pipe(
      exhaustMap(outboxPayloadId =>
        this.administration.requeue(outboxPayloadId).pipe(
          map(
            () =>
              ({ status: 'success', outboxPayloadId }) as const,
          ),
          startWith({ status: 'requeuing', outboxPayloadId } as const),
          catchError(error =>
            of({
              status: 'error',
              outboxPayloadId,
              message: toUserMessage(error),
            } as const),
          ),
        ),
      ),
      startWith({ status: 'idle' } as const),
    );

  protected readonly vm$ = combineLatest({
    request: this.request$,
    payloads: this.payloads$,
    requeue: this.requeue$,
    requeueAll: this.requeueAll$,
  }).pipe(shareReplay({ bufferSize: 1, refCount: true }));

  protected filters(
    request: OutboxPayloadSearchRequest,
  ): readonly ListFilter[] {
    return [
      {
        kind: 'select',
        key: 'onlyIncomplete',
        label: 'Delivery',
        value: String(request.onlyIncomplete),
        options: COMPLETION_FILTER_OPTIONS,
      },
      {
        kind: 'select',
        key: 'state',
        label: 'State',
        value: request.state,
        options: STATE_FILTER_OPTIONS,
      },
      {
        kind: 'text',
        key: 'aggregateId',
        label: 'Aggregate ID',
        value: request.aggregateId,
        placeholder: 'For example aaaaaaaa-aaaa-…',
        maxLength: 500,
      },
    ];
  }

  protected toggleDetails(outboxPayloadId: string): void {
    this.expandedPayloadId.update(currentId =>
      currentId === outboxPayloadId ? null : outboxPayloadId,
    );
  }

  protected requeue(event: Event, outboxPayloadId: string): void {
    event.stopPropagation();
    this.requeueRequests.next(outboxPayloadId);
  }

  protected requeueAll(): void {
    this.requeueAllRequests.next();
  }

  protected search(
    request: OutboxPayloadSearchRequest,
    search: string,
  ): void {
    this.navigate({ ...request, page: 1, search }, true);
  }

  protected filter(
    request: OutboxPayloadSearchRequest,
    change: ListFilterChange,
  ): void {
    if (change.key === 'aggregateId') {
      this.navigate({ ...request, page: 1, aggregateId: change.value }, true);
    } else if (change.key === 'onlyIncomplete') {
      this.navigate({
        ...request,
        page: 1,
        onlyIncomplete: change.value === 'true',
      });
    } else if (change.key === 'state') {
      this.navigate({ ...request, page: 1, state: change.value });
    }
  }

  protected sort(
    request: OutboxPayloadSearchRequest,
    sortBy: string,
  ): void {
    if (!OUTBOX_SORT_OPTIONS.some(option => option.value === sortBy)) return;

    this.navigate({
      ...request,
      page: 1,
      sortBy: sortBy as OutboxPayloadSortField,
    });
  }

  protected changeDirection(
    request: OutboxPayloadSearchRequest,
    sortDirection: ListSortDirection,
  ): void {
    this.navigate({ ...request, page: 1, sortDirection });
  }

  protected changePageSize(
    request: OutboxPayloadSearchRequest,
    pageSize: number,
  ): void {
    this.navigate({ ...request, page: 1, pageSize });
  }

  protected goToPage(
    request: OutboxPayloadSearchRequest,
    page: number,
  ): void {
    if (page < 1 || page === request.page) return;

    this.navigate({ ...request, page });
  }

  private navigate(
    request: OutboxPayloadSearchRequest,
    replaceUrl = false,
  ): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: outboxSearchQueryParams(request),
      replaceUrl,
    });
  }
}
