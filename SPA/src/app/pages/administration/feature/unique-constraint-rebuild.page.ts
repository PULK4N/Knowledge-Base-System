import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
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
import { UniqueConstraintRebuildResult } from '../data-access/unique-constraint-administration.models';
import { UniqueConstraintAdministrationService } from '../data-access/unique-constraint-administration.service';

type RebuildState =
  | { readonly status: 'idle' }
  | { readonly status: 'rebuilding'; readonly stateMachineId: string }
  | {
      readonly status: 'done';
      readonly stateMachineId: string;
      readonly result: UniqueConstraintRebuildResult;
    }
  | {
      readonly status: 'error';
      readonly stateMachineId: string;
      readonly message: string;
    };

interface UniqueConstraintRebuildVm {
  readonly stateMachines: LoadState<readonly string[]>;
  readonly rebuild: RebuildState;
}

/**
 * Recalculates the stored unique constraints of every aggregate in one
 * state machine. Rendered below the projection panels on the Projections tab.
 */
@Component({
  selector: 'app-unique-constraint-rebuild-page',
  imports: [AsyncPipe, ReactiveFormsModule],
  templateUrl: './unique-constraint-rebuild.page.html',
  styleUrl: './unique-constraint-rebuild.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UniqueConstraintRebuildPage {
  private readonly administration = inject(
    UniqueConstraintAdministrationService,
  );
  private readonly rebuildRequests = new Subject<string>();

  protected readonly stateMachineId = new FormControl('', {
    nonNullable: true,
  });

  private readonly stateMachines$: Observable<LoadState<readonly string[]>> =
    this.administration.listStateMachines().pipe(
      map(data => ({ status: 'success', data }) as const),
      startWith({ status: 'loading' } as const),
      catchError(error =>
        of({ status: 'error', message: toUserMessage(error) } as const),
      ),
    );

  private readonly rebuild$: Observable<RebuildState> =
    this.rebuildRequests.pipe(
      exhaustMap(stateMachineId =>
        this.administration.rebuild(stateMachineId).pipe(
          map(result => ({ status: 'done', stateMachineId, result }) as const),
          startWith({ status: 'rebuilding', stateMachineId } as const),
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

  protected readonly vm$: Observable<UniqueConstraintRebuildVm> =
    combineLatest({
      stateMachines: this.stateMachines$,
      rebuild: this.rebuild$,
    }).pipe(shareReplay({ bufferSize: 1, refCount: true }));

  protected rebuild(): void {
    const stateMachineId = this.stateMachineId.value;
    if (stateMachineId) this.rebuildRequests.next(stateMachineId);
  }
}
