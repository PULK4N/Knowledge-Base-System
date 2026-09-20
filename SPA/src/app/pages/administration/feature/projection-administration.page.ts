import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import {
  Observable,
  Subject,
  catchError,
  combineLatest,
  exhaustMap,
  map,
  of,
  shareReplay,
  startWith,
} from 'rxjs';
import { LoadState, toUserMessage } from '../../../core/http/load-state';
import { ProjectionGroup } from '../data-access/projection-administration.models';
import { ProjectionAdministrationService } from '../data-access/projection-administration.service';
import { ProjectionRunnerPage } from './projection-runner.page';

type ExecutionState =
  | { readonly status: 'idle' }
  | { readonly status: 'executing'; readonly stateMachineId: string }
  | {
      readonly status: 'success';
      readonly stateMachineId: string;
      readonly queuedAggregateCount: number;
    }
  | {
      readonly status: 'error';
      readonly stateMachineId: string;
      readonly message: string;
    };

type CacheClearState =
  | { readonly status: 'idle' }
  | { readonly status: 'clearing' }
  | { readonly status: 'success'; readonly deletedCount: number }
  | { readonly status: 'error'; readonly message: string };

interface ProjectionAdministrationVm {
  readonly projections: LoadState<readonly ProjectionGroup[]>;
  readonly execution: ExecutionState;
  readonly cacheClear: CacheClearState;
}

@Component({
  selector: 'app-projection-administration-page',
  imports: [AsyncPipe, ProjectionRunnerPage],
  templateUrl: './projection-administration.page.html',
  styleUrl: './projection-administration.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectionAdministrationPage {
  private readonly administration = inject(ProjectionAdministrationService);
  private readonly executionRequests = new Subject<string>();
  private readonly cacheClearRequests = new Subject<void>();

  private readonly projections$: Observable<
    LoadState<readonly ProjectionGroup[]>
  > = this.administration.list().pipe(
    map(data => ({ status: 'success', data }) as const),
    startWith({ status: 'loading' } as const),
    catchError(error =>
      of({
        status: 'error',
        message: toUserMessage(error),
      } as const),
    ),
  );

  private readonly execution$: Observable<ExecutionState> =
    this.executionRequests.pipe(
      exhaustMap(stateMachineId =>
        this.administration.execute(stateMachineId).pipe(
          map(
            result =>
              ({
                status: 'success',
                stateMachineId,
                queuedAggregateCount: result.queuedAggregateCount,
              }) as const,
          ),
          startWith({ status: 'executing', stateMachineId } as const),
          catchError(error =>
            of({
              status: 'error',
              stateMachineId,
              message: toUserMessage(error),
            } as const),
          ),
        ),
      ),
      startWith({ status: 'idle' } as const),
    );

  private readonly cacheClear$: Observable<CacheClearState> =
    this.cacheClearRequests.pipe(
      exhaustMap(() =>
        this.administration.clearEmbeddingCache().pipe(
          map(
            deletedCount =>
              ({ status: 'success', deletedCount }) as const,
          ),
          startWith({ status: 'clearing' } as const),
          catchError(error =>
            of({
              status: 'error',
              message: toUserMessage(error),
            } as const),
          ),
        ),
      ),
      startWith({ status: 'idle' } as const),
    );

  protected readonly vm$: Observable<ProjectionAdministrationVm> =
    combineLatest({
      projections: this.projections$,
      execution: this.execution$,
      cacheClear: this.cacheClear$,
    }).pipe(shareReplay({ bufferSize: 1, refCount: true }));

  protected execute(stateMachineId: string): void {
    this.executionRequests.next(stateMachineId);
  }

  protected clearEmbeddingCache(): void {
    this.cacheClearRequests.next();
  }
}
