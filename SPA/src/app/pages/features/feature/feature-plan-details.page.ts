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
  FeaturePlanBranch,
  FeaturePlanBranchMerge,
  FeaturePlanContentRequest,
  FeaturePlanStatus,
  SetFeaturePlanBranchSourcesRequest,
  FeatureRecord,
  UpdateFeaturePlanBranchRequest,
} from '../data-access/feature.models';
import { FeatureService } from '../data-access/feature.service';
import {
  branchSourceOptions,
  describeSources,
  previousPlanOptions,
  sourceFromOptionId,
} from '../ui/feature-branch-graph';
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
  /** Branches this plan's base branches may come from. */
  readonly sourceOptions: readonly FeatureLinkOption[];
  readonly describeSources: (branch: FeaturePlanBranch) => string;
}

interface PlanTarget {
  readonly featureId: string;
  readonly planId: string;
}

type PlanAction =
  | ({ readonly kind: 'update'; readonly request: FeaturePlanContentRequest } & PlanTarget)
  | ({ readonly kind: 'records'; readonly request: FeaturePlanContentRequest } & PlanTarget)
  | ({
      readonly kind: 'status';
      readonly status: FeaturePlanStatus;
      readonly mergedInto: readonly FeaturePlanBranchMerge[];
    } & PlanTarget)
  | ({ readonly kind: 'remove' } & PlanTarget)
  | ({ readonly kind: 'previous-plan'; readonly previousPlanId: string | null } & PlanTarget)
  | ({ readonly kind: 'add-branch'; readonly request: AddFeaturePlanBranchRequest } & PlanTarget)
  | ({ readonly kind: 'update-branch'; readonly request: UpdateFeaturePlanBranchRequest } & PlanTarget)
  | ({ readonly kind: 'remove-branch'; readonly branchName: string } & PlanTarget)
  | ({ readonly kind: 'branch-sources'; readonly request: SetFeaturePlanBranchSourcesRequest } & PlanTarget);

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
  /** Set while the user picks where the branches of the plan being completed were merged. */
  protected readonly completing = signal(false);
  /** Merge target per base branch name: a source option ID, or '' for its own sources. */
  protected readonly mergeTargets = signal<ReadonlyMap<string, string>>(new Map());
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
      tap(() => {
        this.completing.set(false);
        this.mergeTargets.set(new Map());
      }),
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
                    sourceOptions: branchSourceOptions(feature, plan.id),
                    describeSources: (branch: FeaturePlanBranch) =>
                      describeSources(branch.sources, feature),
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
          if (action.kind === 'status') this.completing.set(false);
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

  protected changeStatus(
    target: PlanTarget,
    plan: FeaturePlan,
    status: FeaturePlanStatus,
  ): void {
    if (status === 'Completed' && plan.branches.length > 0) {
      this.mergeTargets.set(new Map());
      this.completing.set(true);
      return;
    }

    this.completing.set(false);
    this.actions.next({ kind: 'status', ...target, status, mergedInto: [] });
  }

  protected setMergeTarget(branchName: string, optionId: string): void {
    this.mergeTargets.update(current => new Map(current).set(branchName, optionId));
  }

  protected completePlan(target: PlanTarget): void {
    const mergedInto = [...this.mergeTargets()]
      .filter(([, optionId]) => optionId !== '')
      .map(([branchName, optionId]) => {
        const into = sourceFromOptionId(optionId);
        return {
          branchName,
          intoPlanId: into.planId,
          intoBranchName: into.branchName,
        };
      });

    this.actions.next({
      kind: 'status',
      ...target,
      status: 'Completed',
      mergedInto,
    });
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

  protected setBranchSources(
    target: PlanTarget,
    request: SetFeaturePlanBranchSourcesRequest,
  ): void {
    this.actions.next({ kind: 'branch-sources', ...target, request });
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
          action.mergedInto,
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
      case 'branch-sources':
        return this.features.setPlanBranchSources(
          action.featureId,
          action.planId,
          action.request,
        );
    }
  }
}
