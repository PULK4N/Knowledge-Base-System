import { AsyncPipe, DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
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
  tap,
} from 'rxjs';
import { LoadState, toUserMessage } from '../../../core/http/load-state';
import { ShortIdPipe } from '../../skills/ui/short-id.pipe';
import { Feature, FeaturePlan } from '../data-access/feature.models';
import { FeatureService } from '../data-access/feature.service';

interface FeaturePlansView {
  readonly feature: Feature;
  readonly currentPlan: FeaturePlan | null;
  readonly previousPlans: readonly FeaturePlan[];
}

type PlanAction =
  | {
      readonly kind: 'add';
      readonly featureId: string;
      readonly title: string;
      readonly content: string;
      readonly contentType: 'Markdown' | 'Html';
    }
  | { readonly kind: 'select'; readonly featureId: string; readonly planId: string }
  | { readonly kind: 'remove'; readonly featureId: string; readonly planId: string };

type MutationState =
  | { readonly status: 'idle' }
  | { readonly status: 'saving'; readonly kind: PlanAction['kind'] }
  | { readonly status: 'error'; readonly message: string };

@Component({
  selector: 'app-feature-plans-page',
  imports: [AsyncPipe, DatePipe, FormsModule, RouterLink, ShortIdPipe],
  templateUrl: './feature-plans.page.html',
  styleUrls: ['./feature-plans.page.css', '../ui/feature-pages.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FeaturePlansPage {
  private readonly route = inject(ActivatedRoute);
  private readonly features = inject(FeatureService);
  private readonly actions = new Subject<PlanAction>();

  protected readonly addingPlan = signal(false);
  protected readonly confirmingPlanRemoval = signal<string | null>(null);

  private readonly state$: Observable<LoadState<FeaturePlansView>> =
    this.route.paramMap.pipe(
      map(params => params.get('featureId')),
      filter((featureId): featureId is string => featureId !== null),
      distinctUntilChanged(),
      switchMap(featureId =>
        this.features.watch(featureId).pipe(
          map(feature => ({
            status: 'success',
            data: {
              feature,
              currentPlan:
                feature.plans.find(
                  plan => plan.id === feature.currentPlanId,
                ) ?? null,
              previousPlans: feature.plans.filter(
                plan => plan.id !== feature.currentPlanId,
              ),
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

  private readonly mutation$: Observable<MutationState> = this.actions.pipe(
    exhaustMap(action =>
      this.execute(action).pipe(
        tap(() => {
          this.addingPlan.set(false);
          this.confirmingPlanRemoval.set(null);
        }),
        map(() => ({ status: 'idle' }) as const),
        startWith({ status: 'saving', kind: action.kind } as const),
        catchError(error =>
          of({ status: 'error', message: toUserMessage(error) } as const),
        ),
      ),
    ),
    startWith({ status: 'idle' } as const),
  );

  protected readonly vm$ = combineLatest({
    state: this.state$,
    mutation: this.mutation$,
  }).pipe(shareReplay({ bufferSize: 1, refCount: true }));

  protected addPlan(
    featureId: string,
    title: string,
    content: string,
    contentType: 'Markdown' | 'Html',
  ): void {
    this.actions.next({
      kind: 'add',
      featureId,
      title: title.trim(),
      content,
      contentType,
    });
  }

  protected selectPlan(featureId: string, planId: string): void {
    this.actions.next({ kind: 'select', featureId, planId });
  }

  protected removePlan(featureId: string, planId: string): void {
    this.actions.next({ kind: 'remove', featureId, planId });
  }

  private execute(action: PlanAction): Observable<Feature> {
    switch (action.kind) {
      case 'add':
        return this.features.addPlan(action.featureId, {
          title: action.title,
          content: action.content,
          contentType: action.contentType,
        });
      case 'select':
        return this.features.changeCurrentPlan(action.featureId, action.planId);
      case 'remove':
        return this.features.removePlan(action.featureId, action.planId);
    }
  }
}
