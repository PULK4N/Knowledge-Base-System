import { Observable, catchError, exhaustMap, map, of, startWith, tap } from 'rxjs';
import { toUserMessage } from '../../../core/http/load-state';

export type MutationState<TKind extends string> =
  | { readonly status: 'idle' }
  | { readonly status: 'saving'; readonly kind: TKind }
  | { readonly status: 'error'; readonly kind: TKind; readonly message: string };

export interface MutationRequest<TKind extends string> {
  readonly kind: TKind;
  readonly request: Observable<unknown>;
  /** Runs after the request succeeds, for example to close an editor. */
  readonly done?: () => void;
}

/** Runs one write at a time and reports it as idle, saving or error. */
export function mutationStates<TKind extends string>(
  requests: Observable<MutationRequest<TKind>>,
): Observable<MutationState<TKind>> {
  return requests.pipe(
    exhaustMap(({ kind, request, done }) =>
      request.pipe(
        tap(() => done?.()),
        map((): MutationState<TKind> => ({ status: 'idle' })),
        startWith<MutationState<TKind>>({ status: 'saving', kind }),
        catchError(error =>
          of<MutationState<TKind>>({
            status: 'error',
            kind,
            message: toUserMessage(error),
          }),
        ),
      ),
    ),
    startWith<MutationState<TKind>>({ status: 'idle' }),
  );
}
