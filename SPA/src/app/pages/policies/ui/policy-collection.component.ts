import { AsyncPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
  signal,
} from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  Observable,
  Subject,
  catchError,
  combineLatest,
  distinctUntilChanged,
  exhaustMap,
  map,
  of,
  shareReplay,
  startWith,
  switchMap,
  tap,
} from 'rxjs';
import { toUserMessage } from '../../../core/http/load-state';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { MarkdownContentComponent } from '../../skills/ui/markdown-content.component';
import {
  Policy,
  PolicyScope,
  PolicySearchResult,
} from '../data-access/policy.models';
import { PolicyService } from '../data-access/policy.service';
import { policyHistoryLink } from '../feature/policy-history';
import { policySearchRequests } from '../feature/policy-search-requests';

type PolicyAction =
  | {
      readonly kind: 'update';
      readonly scope: PolicyScope;
      readonly policy: Policy;
    }
  | {
      readonly kind: 'remove';
      readonly scope: PolicyScope;
      readonly policyId: string;
    };

type PolicyMutationState =
  | { readonly status: 'idle' }
  | {
      readonly status: 'saving' | 'deleting';
      readonly policyId: string;
    }
  | {
      readonly status: 'error';
      readonly policyId: string;
      readonly message: string;
    };

const EMPTY_RESULT: PolicySearchResult = {
  items: [],
  page: 1,
  pageSize: 5,
  totalCount: 0,
  totalPages: 0,
  hasPreviousPage: false,
  hasNextPage: false,
};

/** Searchable, paged policy list of one scope with inline edit and delete. */
@Component({
  selector: 'app-policy-collection',
  imports: [
    AsyncPipe,
    FormsModule,
    MarkdownContentComponent,
    PaginationComponent,
    RouterLink,
  ],
  templateUrl: './policy-collection.component.html',
  styleUrls: ['../feature/policy-list.page.css', './knowledge-list.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PolicyCollectionComponent {
  private readonly policies = inject(PolicyService);
  private readonly searchRequests = new Subject<string>();
  private readonly pageRequests = new Subject<number>();
  private readonly request$ = policySearchRequests(
    this.searchRequests,
    this.pageRequests,
  );
  private readonly actions = new Subject<PolicyAction>();

  readonly scope = input.required<PolicyScope>();

  protected readonly emptyBlocks = [];
  protected readonly editingPolicyId = signal<string | null>(null);
  protected readonly confirmingPolicyId = signal<string | null>(null);
  protected readonly searchText = signal('');

  private readonly result$: Observable<PolicySearchResult> = combineLatest({
    scope: toObservable(this.scope).pipe(
      distinctUntilChanged(
        (previous, current) =>
          JSON.stringify(previous) === JSON.stringify(current),
      ),
    ),
    request: this.request$,
  }).pipe(
    switchMap(({ scope, request }) =>
      this.policies.searchPolicies(scope, request).pipe(
        catchError(() => of(EMPTY_RESULT)),
      ),
    ),
    startWith(EMPTY_RESULT),
  );

  private readonly mutation$: Observable<PolicyMutationState> =
    this.actions.pipe(
      exhaustMap(action => {
        if (action.kind === 'update') {
          return this.policies.updatePolicy(action.scope, {
            policyId: action.policy.id,
            title: action.policy.title,
            description: action.policy.description,
          }).pipe(
            tap(() => this.editingPolicyId.set(null)),
            map(() => ({ status: 'idle' }) as const),
            startWith({
              status: 'saving',
              policyId: action.policy.id,
            } as const),
            catchError(error =>
              of({
                status: 'error',
                policyId: action.policy.id,
                message: toUserMessage(error),
              } as const),
            ),
          );
        }

        return this.policies.removePolicy(action.scope, action.policyId).pipe(
          tap(() => this.confirmingPolicyId.set(null)),
          map(() => ({ status: 'idle' }) as const),
          startWith({
            status: 'deleting',
            policyId: action.policyId,
          } as const),
          catchError(error =>
            of({
              status: 'error',
              policyId: action.policyId,
              message: toUserMessage(error),
            } as const),
          ),
        );
      }),
      startWith({ status: 'idle' } as const),
    );

  protected readonly vm$ = combineLatest({
    result: this.result$,
    mutation: this.mutation$,
  }).pipe(shareReplay({ bufferSize: 1, refCount: true }));

  protected historyLink(
    scope: PolicyScope,
    policyId: string,
  ): readonly string[] {
    return policyHistoryLink(scope, policyId);
  }

  protected search(search: string): void {
    this.searchText.set(search);
    this.searchRequests.next(search);
  }

  protected goToPage(page: number): void {
    this.pageRequests.next(page);
  }

  protected startEditing(policy: Policy): void {
    this.confirmingPolicyId.set(null);
    this.editingPolicyId.set(policy.id);
  }

  protected cancelEditing(): void {
    this.editingPolicyId.set(null);
  }

  protected updatePolicy(
    scope: PolicyScope,
    policyId: string,
    title: string,
    description: string,
  ): void {
    this.actions.next({
      kind: 'update',
      scope,
      policy: { id: policyId, title: title.trim(), description },
    });
  }

  protected startRemoving(policyId: string): void {
    this.editingPolicyId.set(null);
    this.confirmingPolicyId.set(policyId);
  }

  protected cancelRemoving(): void {
    this.confirmingPolicyId.set(null);
  }

  protected removePolicy(scope: PolicyScope, policyId: string): void {
    this.actions.next({ kind: 'remove', scope, policyId });
  }
}
