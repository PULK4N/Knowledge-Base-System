import { AsyncPipe, DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import {
  BehaviorSubject,
  Observable,
  Subject,
  catchError,
  combineLatest,
  exhaustMap,
  map,
  of,
  shareReplay,
  startWith,
  switchMap,
} from 'rxjs';
import { LoadState, toUserMessage } from '../../../core/http/load-state';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import {
  OutboxPayloadSearchRequest,
  OutboxPayloadSearchResult,
} from '../data-access/outbox-administration.models';
import { OutboxAdministrationService } from '../data-access/outbox-administration.service';

const PAGE_SIZE = 10;

type RequeueState =
  | { readonly status: 'idle' }
  | { readonly status: 'requeuing'; readonly outboxPayloadId: string }
  | { readonly status: 'success'; readonly outboxPayloadId: string }
  | {
      readonly status: 'error';
      readonly outboxPayloadId: string;
      readonly message: string;
    };

interface OutboxAdministrationVm {
  readonly payloads: LoadState<OutboxPayloadSearchResult>;
  readonly requeue: RequeueState;
  readonly onlyIncomplete: boolean;
}

@Component({
  selector: 'app-outbox-administration-page',
  imports: [AsyncPipe, DatePipe, PaginationComponent],
  templateUrl: './outbox-administration.page.html',
  styleUrl: './outbox-administration.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OutboxAdministrationPage {
  private readonly administration = inject(OutboxAdministrationService);
  private readonly querySubject =
    new BehaviorSubject<OutboxPayloadSearchRequest>({
      page: 1,
      pageSize: PAGE_SIZE,
      onlyIncomplete: false,
    });
  private readonly requeueRequests = new Subject<string>();
  protected readonly expandedPayloadId = signal<string | null>(null);

  private readonly payloads$: Observable<
    LoadState<OutboxPayloadSearchResult>
  > = this.querySubject.pipe(
    switchMap(request =>
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

  protected readonly vm$: Observable<OutboxAdministrationVm> =
    combineLatest({
      payloads: this.payloads$,
      requeue: this.requeue$,
      query: this.querySubject,
    }).pipe(
      map(({ payloads, requeue, query }) => ({
        payloads,
        requeue,
        onlyIncomplete: query.onlyIncomplete,
      })),
      shareReplay({ bufferSize: 1, refCount: true }),
    );

  protected toggleDetails(outboxPayloadId: string): void {
    this.expandedPayloadId.update(currentId =>
      currentId === outboxPayloadId ? null : outboxPayloadId,
    );
  }

  protected requeue(event: Event, outboxPayloadId: string): void {
    event.stopPropagation();
    this.requeueRequests.next(outboxPayloadId);
  }

  protected goToPage(page: number): void {
    const current = this.querySubject.value;
    if (page < 1 || page === current.page) return;

    this.querySubject.next({ ...current, page });
  }

  protected showOnlyIncomplete(onlyIncomplete: boolean): void {
    this.querySubject.next({
      ...this.querySubject.value,
      page: 1,
      onlyIncomplete,
    });
  }
}
