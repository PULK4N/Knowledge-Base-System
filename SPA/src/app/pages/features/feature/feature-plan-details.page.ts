import { AsyncPipe, DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
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
import { FeaturePlanContentComponent } from '../ui/feature-plan-content.component';

interface FeaturePlanDetailsView {
  readonly feature: Feature;
  readonly plan: FeaturePlan;
  readonly isCurrent: boolean;
}

type PlanAction =
  | {
      readonly kind: 'update';
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
  selector: 'app-feature-plan-details-page',
  imports: [
    AsyncPipe,
    DatePipe,
    FeaturePlanContentComponent,
    FormsModule,
    RouterLink,
    ShortIdPipe,
  ],
  templateUrl: './feature-plan-details.page.html',
  styleUrls: ['./feature-plan-details.page.css', '../ui/feature-pages.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FeaturePlanDetailsPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly features = inject(FeatureService);
  private readonly actions = new Subject<PlanAction>();

  protected readonly editing = signal(false);
  protected readonly confirmingRemoval = signal(false);

  private readonly state$: Observable<LoadState<FeaturePlanDetailsView>> =
    this.route.paramMap.pipe(
      map(params => ({
        featureId: params.get('featureId'),
        planId: params.get('planId'),
      })),
      filter(
        (ids): ids is { featureId: string; planId: string } =>
          ids.featureId !== null && ids.planId !== null,
      ),
      distinctUntilChanged(
        (previous, current) =>
          previous.featureId === current.featureId &&
          previous.planId === current.planId,
      ),
      switchMap(({ featureId, planId }) =>
        this.features.watch(featureId).pipe(
          map(feature => {
            const plan = feature.plans.find(item => item.id === planId);

            return plan
              ? ({
                  status: 'success',
                  data: {
                    feature,
                    plan,
                    isCurrent: feature.currentPlanId === plan.id,
                  },
                } as const)
              : ({
                  status: 'error',
                  message: 'The requested plan could not be found.',
                } as const);
          }),
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
          if (action.kind === 'update') this.editing.set(false);
          if (action.kind === 'remove') {
            void this.router.navigate(['/features', action.featureId, 'plans']);
          }
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

  protected updatePlan(
    featureId: string,
    title: string,
    content: string,
    contentType: 'Markdown' | 'Html',
  ): void {
    this.actions.next({
      kind: 'update',
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
      case 'update':
        return this.features.updateCurrentPlan(action.featureId, {
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
