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
import {
  AddFeaturePlanBranchRequest,
  Feature,
  FeaturePlan,
  FeaturePlanContentRequest,
  FeaturePlanStatus,
  FeatureRecord,
  UpdateFeaturePlanBranchRequest,
} from '../data-access/feature.models';
import { FeatureService } from '../data-access/feature.service';
import { previousPlanOptions } from '../ui/feature-branch-tree';
import {
  FeatureLinkOption,
  FeatureLinkPickerComponent,
} from '../ui/feature-link-picker.component';
import { FeaturePlanBranchTreeComponent } from '../ui/feature-plan-branch-tree.component';
import { FeaturePlanContentComponent } from '../ui/feature-plan-content.component';
import {
  FEATURE_PLAN_STATUS_OPTIONS,
  FeaturePlanStatusPipe,
} from '../ui/feature-plan-status';
import { toRecordOptions } from '../ui/feature-link-options';

interface FeaturePlanDetailsView {
  readonly feature: Feature;
  readonly plan: FeaturePlan;
  readonly previousPlanOptions: readonly FeaturePlan[];
  /** Linked records in link order; IDs of removed records are left out. */
  readonly linkedRecords: readonly FeatureRecord[];
  readonly recordOptions: readonly FeatureLinkOption[];
}

interface PlanTarget {
  readonly featureId: string;
  readonly planId: string;
}

type PlanAction =
  | ({ readonly kind: 'update'; readonly request: FeaturePlanContentRequest } & PlanTarget)
  | ({ readonly kind: 'records'; readonly request: FeaturePlanContentRequest } & PlanTarget)
  | ({ readonly kind: 'status'; readonly status: FeaturePlanStatus } & PlanTarget)
  | ({ readonly kind: 'remove' } & PlanTarget)
  | ({ readonly kind: 'previous-plan'; readonly previousPlanId: string | null } & PlanTarget)
  | ({ readonly kind: 'add-branch'; readonly request: AddFeaturePlanBranchRequest } & PlanTarget)
  | ({ readonly kind: 'update-branch'; readonly request: UpdateFeaturePlanBranchRequest } & PlanTarget)
  | ({ readonly kind: 'remove-branch'; readonly branchName: string } & PlanTarget);

type MutationState =
  | { readonly status: 'idle' }
  | { readonly status: 'saving'; readonly kind: PlanAction['kind'] }
  | { readonly status: 'error'; readonly message: string };

@Component({
  selector: 'app-feature-plan-details-page',
  imports: [
    AsyncPipe,
    DatePipe,
    FeatureLinkPickerComponent,
    FeaturePlanBranchTreeComponent,
    FeaturePlanContentComponent,
    FeaturePlanStatusPipe,
    FormsModule,
    RouterLink,
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
  protected readonly editingRecords = signal(false);
  /** Records picked while editing the plan's links. */
  protected readonly pickedRecordIds = signal<readonly string[]>([]);
  protected readonly confirmingRemoval = signal(false);
  protected readonly statusOptions = FEATURE_PLAN_STATUS_OPTIONS;

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
                    previousPlanOptions: previousPlanOptions(feature.plans, plan.id),
                    linkedRecords: plan.recordIds
                      .map(recordId =>
                        feature.records.find(record => record.id === recordId),
                      )
                      .filter((record): record is FeatureRecord => !!record),
                    recordOptions: toRecordOptions(feature.records),
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
          if (action.kind === 'records') this.editingRecords.set(false);
          if (action.kind === 'remove') {
            void this.router.navigate(['/features', action.featureId], {
              queryParams: { tab: 'plans' },
            });
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
    target: PlanTarget,
    title: string,
    status: FeaturePlanStatus,
    additionalInfo: string,
    contentType: 'Markdown' | 'Html',
    content: string,
  ): void {
    this.actions.next({
      kind: 'update',
      ...target,
      request: {
        title: title.trim(),
        status,
        additionalInfo: additionalInfo.trim(),
        contentType,
        content,
      },
    });
  }

  protected startEditingRecords(plan: FeaturePlan): void {
    this.pickedRecordIds.set(plan.recordIds);
    this.editingRecords.set(true);
  }

  /** Saves the picked links through a plan update that keeps the plan's content. */
  protected saveRecordLinks(target: PlanTarget, plan: FeaturePlan): void {
    this.actions.next({
      kind: 'records',
      ...target,
      request: {
        title: plan.title,
        status: plan.status,
        additionalInfo: plan.additionalInfo,
        contentType: plan.contentType,
        content: plan.content,
        recordIds: this.pickedRecordIds(),
      },
    });
  }

  protected changeStatus(target: PlanTarget, status: FeaturePlanStatus): void {
    this.actions.next({ kind: 'status', ...target, status });
  }

  protected setPreviousPlan(target: PlanTarget, previousPlanId: string): void {
    this.actions.next({
      kind: 'previous-plan',
      ...target,
      previousPlanId: previousPlanId || null,
    });
  }

  protected removePlan(target: PlanTarget): void {
    this.actions.next({ kind: 'remove', ...target });
  }

  protected addBranch(
    target: PlanTarget,
    request: AddFeaturePlanBranchRequest,
  ): void {
    this.actions.next({ kind: 'add-branch', ...target, request });
  }

  protected updateBranch(
    target: PlanTarget,
    request: UpdateFeaturePlanBranchRequest,
  ): void {
    this.actions.next({ kind: 'update-branch', ...target, request });
  }

  protected removeBranch(target: PlanTarget, branchName: string): void {
    this.actions.next({ kind: 'remove-branch', ...target, branchName });
  }

  private execute(action: PlanAction): Observable<Feature> {
    switch (action.kind) {
      case 'update':
      case 'records':
        return this.features.updatePlan(
          action.featureId,
          action.planId,
          action.request,
        );
      case 'status':
        return this.features.changePlanStatus(
          action.featureId,
          action.planId,
          action.status,
        );
      case 'remove':
        return this.features.removePlan(action.featureId, action.planId);
      case 'previous-plan':
        return this.features.setPlanPreviousPlan(
          action.featureId,
          action.planId,
          action.previousPlanId,
        );
      case 'add-branch':
        return this.features.addPlanBranch(
          action.featureId,
          action.planId,
          action.request,
        );
      case 'update-branch':
        return this.features.updatePlanBranch(
          action.featureId,
          action.planId,
          action.request,
        );
      case 'remove-branch':
        return this.features.removePlanBranch(
          action.featureId,
          action.planId,
          action.branchName,
        );
    }
  }
}
