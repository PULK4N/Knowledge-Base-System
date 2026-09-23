import { AsyncPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
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
import { PolicyScope, PolicyWithHistory } from '../data-access/policy.models';
import { PolicyService } from '../data-access/policy.service';
import { policyScopeFromRoute } from './policy-list.page';
import {
  PolicyHistoryItem,
  policyListLink,
  toPolicyHistory,
} from './policy-history';

interface PolicyHistoryView {
  readonly policy: PolicyWithHistory;
  readonly history: readonly PolicyHistoryItem[];
}

@Component({
  selector: 'app-policy-history-page',
  imports: [AsyncPipe, DatePipe, RouterLink],
  templateUrl: './policy-history.page.html',
  styleUrls: ['./policy-history.page.css', '../ui/knowledge-list.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PolicyHistoryPage {
  private readonly route = inject(ActivatedRoute);
  private readonly policies = inject(PolicyService);

  private readonly target$ = combineLatest([
    this.route.data,
    this.route.paramMap,
  ]).pipe(
    map(([data, params]) => ({
      scope: policyScopeFromRoute(data['policyScope'], params),
      policyId: params.get('policyId'),
    })),
    distinctUntilChanged(
      (previous, current) => JSON.stringify(previous) === JSON.stringify(current),
    ),
  );

  protected readonly vm$: Observable<{
    readonly backLink: readonly string[];
    readonly state: LoadState<PolicyHistoryView>;
  }> = this.target$.pipe(
    switchMap(({ scope, policyId }) => {
      const backLink = scope ? policyListLink(scope) : ['/policies'];
      const state$: Observable<LoadState<PolicyHistoryView>> =
        scope && policyId
          ? this.load(scope, policyId)
          : of({
              status: 'error',
              message: 'The requested policy could not be found.',
            } as const);

      return state$.pipe(map(state => ({ backLink, state })));
    }),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  private load(
    scope: PolicyScope,
    policyId: string,
  ): Observable<LoadState<PolicyHistoryView>> {
    return this.policies.watchPolicy(scope, policyId).pipe(
      map(
        policy =>
          ({
            status: 'success',
            data: { policy, history: toPolicyHistory(policy.memoryHistory) },
          }) as const,
      ),
      startWith({ status: 'loading' } as const),
      catchError(error =>
        of({ status: 'error', message: toUserMessage(error) } as const),
      ),
    );
  }
}
