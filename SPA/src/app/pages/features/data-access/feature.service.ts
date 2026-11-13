import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import {
  Observable,
  filter,
  ignoreElements,
  map,
  merge,
  switchMap,
  tap,
} from 'rxjs';
import {
  EntityStore,
  PagedResult,
} from '../../../core/store/entity-store.service';
import {
  AddFeaturePlanBranchRequest,
  AddFeaturePlanRequest,
  AddFeatureRequest,
  Feature,
  FeatureCommandResult,
  FeatureCreatedCommandResult,
  FeatureDto,
  FeatureSearchRequest,
  FeatureSearchResult,
  FeatureSummary,
  FeatureSummaryDto,
  FeaturePlanContentRequest,
  FeaturePlanCreatedCommandResult,
  FeaturePlanStatus,
  SetFeatureBranchRequest,
  SetFeatureCompositeBranchRequest,
  SetFeaturePlanBranchSourcesRequest,
  FeaturePlanBranchMerge,
  AddFeatureRecordRequest,
  FeatureRecordCreatedCommandResult,
  FeatureReviewNoteContentRequest,
  FeatureReviewerContentRequest,
  FeatureReviewerCreatedCommandResult,
  UpdateFeatureReviewerRequest,
  FeatureReviewNoteCreatedCommandResult,
  UpdateFeaturePlanBranchRequest,
  UpdateFeatureRecordRequest,
  UpdateFeatureReviewNoteRequest,
  EMPTY_SPECIFICATION,
  PlanReviewAction,
  PlaywrightScriptSnapshot,
  SpecificationAction,
} from './feature.models';

const FEATURE_ENTITY_TYPE = 'feature';

function isFeature(entity: FeatureSummary | undefined): entity is Feature {
  return (
    !!entity &&
    'records' in entity &&
    'reviewNotes' in entity &&
    'plans' in entity
  );
}

@Injectable({ providedIn: 'root' })
export class FeatureService {
  private readonly http = inject(HttpClient);
  private readonly store = inject(EntityStore);
  private readonly controllerPath = '/api/features';

  search(request: FeatureSearchRequest): Observable<FeatureSearchResult> {
    const normalizedSearch = request.search.trim();
    const normalizedProjectId = request.projectId.trim();
    const queryKey = JSON.stringify({
      entityType: FEATURE_ENTITY_TYPE,
      page: request.page,
      pageSize: request.pageSize,
      search: normalizedSearch.toLowerCase(),
      projectId: normalizedProjectId.toLowerCase(),
      sortBy: request.sortBy,
      sortDirection: request.sortDirection,
    });
    let params = new HttpParams()
      .set('page', request.page)
      .set('pageSize', request.pageSize)
      .set('sortBy', request.sortBy)
      .set('sortDirection', request.sortDirection);

    if (normalizedSearch) {
      params = params.set('search', normalizedSearch);
    }

    if (normalizedProjectId) {
      params = params.set('projectId', normalizedProjectId);
    }

    const refresh$ = this.http
      .get<PagedResult<FeatureSummaryDto>>(this.controllerPath, { params })
      .pipe(
        map(result => ({
          ...result,
          items: result.items.map(item => ({
            id: item.featureId,
            projectId: item.projectId,
            name: item.name,
            summary: item.summary,
            status: item.status,
            planCount: item.planCount,
            recordCount: item.recordCount,
          })),
        })),
        tap(result =>
          this.store.replaceSearch(queryKey, FEATURE_ENTITY_TYPE, result),
        ),
        ignoreElements(),
      );

    const cached$ = this.store.search$<FeatureSummary>(queryKey).pipe(
      filter(
        (result): result is FeatureSearchResult => result !== undefined,
      ),
    );

    return merge(cached$, refresh$);
  }

  watch(id: string): Observable<Feature> {
    const refresh$ = this.refresh(id).pipe(ignoreElements());
    const cached$ = this.store
      .entity$<FeatureSummary>(FEATURE_ENTITY_TYPE, id)
      .pipe(filter(isFeature));

    return merge(cached$, refresh$);
  }

  create(request: AddFeatureRequest): Observable<Feature> {
    return this.http
      .post<FeatureCreatedCommandResult>(this.controllerPath, request)
      .pipe(switchMap(result => this.refresh(result.featureId)));
  }

  remove(id: string): Observable<FeatureCommandResult> {
    return this.http
      .post<FeatureCommandResult>(
        `${this.featurePath(id)}/remove`,
        null,
      )
      .pipe(tap(() => this.store.remove(FEATURE_ENTITY_TYPE, id)));
  }

  updateStatus(id: string, status: string, compositeBranchName?: string): Observable<Feature> {
    return this.postAndRefresh(id, 'status', {
      status,
      ...(compositeBranchName === undefined ? {} : { compositeBranchName }),
    });
  }

  updateSummary(id: string, summary: string, compositeBranchName?: string): Observable<Feature> {
    return this.postAndRefresh(id, 'summary', {
      summary,
      ...(compositeBranchName === undefined ? {} : { compositeBranchName }),
    });
  }

  setCompositeBranch(id: string, request: SetFeatureCompositeBranchRequest): Observable<Feature> {
    return this.postAndRefresh(id, 'composite-branch', request);
  }

  addRecord(
    id: string,
    request: AddFeatureRecordRequest,
  ): Observable<Feature> {
    return this.http
      .post<FeatureRecordCreatedCommandResult>(
        `${this.featurePath(id)}/records`,
        request,
      )
      .pipe(switchMap(() => this.refresh(id)));
  }

  updateRecord(
    id: string,
    request: UpdateFeatureRecordRequest,
  ): Observable<Feature> {
    return this.postAndRefresh(id, 'records/update', request);
  }

  removeRecord(id: string, recordId: string): Observable<Feature> {
    return this.postAndRefresh(id, 'records/remove', { recordId });
  }

  addReviewNote(
    id: string,
    request: FeatureReviewNoteContentRequest,
  ): Observable<Feature> {
    return this.http
      .post<FeatureReviewNoteCreatedCommandResult>(
        `${this.featurePath(id)}/review-notes`,
        request,
      )
      .pipe(switchMap(() => this.refresh(id)));
  }

  updateReviewNote(
    id: string,
    request: UpdateFeatureReviewNoteRequest,
  ): Observable<Feature> {
    return this.postAndRefresh(id, 'review-notes/update', request);
  }

  removeReviewNote(id: string, reviewNoteId: string): Observable<Feature> {
    return this.postAndRefresh(id, 'review-notes/remove', { reviewNoteId });
  }

  addReviewer(
    id: string,
    request: FeatureReviewerContentRequest,
  ): Observable<Feature> {
    return this.http
      .post<FeatureReviewerCreatedCommandResult>(
        `${this.featurePath(id)}/reviewers`,
        request,
      )
      .pipe(switchMap(() => this.refresh(id)));
  }

  updateReviewer(
    id: string,
    request: UpdateFeatureReviewerRequest,
  ): Observable<Feature> {
    return this.postAndRefresh(id, 'reviewers/update', request);
  }

  removeReviewer(id: string, reviewerId: string): Observable<Feature> {
    return this.postAndRefresh(id, 'reviewers/remove', { reviewerId });
  }

  addPlan(
    id: string,
    request: AddFeaturePlanRequest,
  ): Observable<Feature> {
    return this.http
      .post<FeaturePlanCreatedCommandResult>(
        `${this.featurePath(id)}/plans`,
        request,
      )
      .pipe(switchMap(() => this.refresh(id)));
  }

  updatePlan(
    id: string,
    planId: string,
    request: FeaturePlanContentRequest,
  ): Observable<Feature> {
    return this.postAndRefresh(id, `${this.planPath(planId)}/update`, request);
  }

  /**
   * Completing a plan moves plans that came from its branches to where those
   * branches were merged; mergedInto names that branch per base branch.
   */
  changePlanStatus(
    id: string,
    planId: string,
    status: FeaturePlanStatus,
    mergedInto: readonly FeaturePlanBranchMerge[] = [],
  ): Observable<Feature> {
    return this.postAndRefresh(id, `${this.planPath(planId)}/status`, {
      status,
      mergedInto,
    });
  }

  setBranch(id: string, request: SetFeatureBranchRequest): Observable<Feature> {
    return this.postAndRefresh(id, 'branch', request);
  }

  setPlanPreviousPlan(
    id: string,
    planId: string,
    previousPlanId: string | null,
  ): Observable<Feature> {
    return this.postAndRefresh(id, `${this.planPath(planId)}/previous-plan`, {
      previousPlanId,
    });
  }

  removePlan(id: string, planId: string): Observable<Feature> {
    return this.postAndRefresh(id, `${this.planPath(planId)}/remove`, {});
  }

  addPlanBranch(
    id: string,
    planId: string,
    request: AddFeaturePlanBranchRequest,
  ): Observable<Feature> {
    return this.postAndRefresh(id, `${this.planPath(planId)}/branches`, request);
  }

  updatePlanBranch(
    id: string,
    planId: string,
    request: UpdateFeaturePlanBranchRequest,
  ): Observable<Feature> {
    return this.postAndRefresh(
      id,
      `${this.planPath(planId)}/branches/update`,
      request,
    );
  }

  removePlanBranch(
    id: string,
    planId: string,
    branchName: string,
  ): Observable<Feature> {
    return this.postAndRefresh(
      id,
      `${this.planPath(planId)}/branches/remove`,
      { branchName },
    );
  }

  setPlanBranchSources(
    id: string,
    planId: string,
    request: SetFeaturePlanBranchSourcesRequest,
  ): Observable<Feature> {
    return this.postAndRefresh(
      id,
      `${this.planPath(planId)}/branches/sources`,
      request,
    );
  }

  /**
   * Changes the specification of a plan, or of the feature when planId is null,
   * and refreshes the feature.
   */
  changeSpecification(
    id: string,
    planId: string | null,
    action: SpecificationAction,
  ): Observable<Feature> {
    const scope = planId === null ? '' : `${this.planPath(planId)}/`;
    const [path, body] = specificationRequest(action);
    return this.postAndRefresh(id, scope + path, body);
  }

  /** Reads one script's snapshot code, which feature reads leave out. */
  getScriptSnapshot(
    id: string,
    planId: string | null,
    scriptId: string,
  ): Observable<PlaywrightScriptSnapshot> {
    const scope = planId === null ? '' : `${this.planPath(planId)}/`;
    return this.http.get<PlaywrightScriptSnapshot>(
      `${this.featurePath(id)}/${scope}playwright-scripts/${encodeURIComponent(scriptId)}/snapshot`,
    );
  }

  /** Changes a plan's own review notes or reviewers and refreshes the feature. */
  changePlanReviews(
    id: string,
    planId: string,
    action: PlanReviewAction,
  ): Observable<Feature> {
    const plan = this.planPath(planId);
    switch (action.kind) {
      case 'add-review-note':
        return this.postAndRefresh(id, `${plan}/review-notes`, action.request);
      case 'update-review-note':
        return this.postAndRefresh(id, `${plan}/review-notes/update`, action.request);
      case 'remove-review-note':
        return this.postAndRefresh(id, `${plan}/review-notes/remove`, {
          reviewNoteId: action.reviewNoteId,
        });
      case 'add-reviewer':
        return this.postAndRefresh(id, `${plan}/reviewers`, action.request);
      case 'update-reviewer':
        return this.postAndRefresh(id, `${plan}/reviewers/update`, action.request);
      case 'remove-reviewer':
        return this.postAndRefresh(id, `${plan}/reviewers/remove`, {
          reviewerId: action.reviewerId,
        });
    }
  }

  private postAndRefresh(
    id: string,
    path: string,
    body: object,
  ): Observable<Feature> {
    return this.http
      .post<FeatureCommandResult>(`${this.featurePath(id)}/${path}`, body)
      .pipe(switchMap(() => this.refresh(id)));
  }

  private planPath(planId: string): string {
    return `plans/${encodeURIComponent(planId)}`;
  }

  private featurePath(id: string): string {
    return `${this.controllerPath}/${encodeURIComponent(id)}`;
  }

  private refresh(id: string): Observable<Feature> {
    return this.http
      .get<FeatureDto>(
        this.featurePath(id),
      )
      .pipe(
        map(feature => ({
          ...feature,
          specification: feature.specification ?? EMPTY_SPECIFICATION,
          plans: feature.plans.map(plan => ({
            ...plan,
            specification: plan.specification ?? EMPTY_SPECIFICATION,
            reviewNotes: plan.reviewNotes ?? [],
            reviewers: plan.reviewers ?? [],
          })),
          reviewers: feature.reviewers ?? [],
          planCount: feature.plans.length,
          recordCount: feature.records.length,
        })),
        tap(feature => this.store.upsert(FEATURE_ENTITY_TYPE, feature)),
      );
  }
}

/** The REST path below the plan or feature and the body of a specification change. */
function specificationRequest(action: SpecificationAction): readonly [string, object] {
  switch (action.kind) {
    case 'add-requirements':
      return ['requirements', { requirements: action.requirements }];
    case 'update-requirements':
      return ['requirements/update', { requirements: action.requirements }];
    case 'remove-requirements':
      return ['requirements/remove', { requirementIds: action.requirementIds }];
    case 'add-definitions-of-done':
      return ['definitions-of-done', { definitionsOfDone: action.definitionsOfDone }];
    case 'update-definitions-of-done':
      return ['definitions-of-done/update', { definitionsOfDone: action.definitionsOfDone }];
    case 'remove-definitions-of-done':
      return ['definitions-of-done/remove', { definitionOfDoneIds: action.definitionOfDoneIds }];
    case 'set-definition-of-done-states':
      return ['definitions-of-done/states', { states: action.states }];
    case 'move-definitions-of-done':
      return [
        'definitions-of-done/move',
        { targetPlanId: action.targetPlanId, definitionOfDoneIds: action.definitionOfDoneIds },
      ];
    case 'add-scripts':
      return ['playwright-scripts', { scripts: action.scripts }];
    case 'update-scripts':
      return ['playwright-scripts/update', { scripts: action.scripts }];
    case 'remove-scripts':
      return ['playwright-scripts/remove', { scriptIds: action.scriptIds }];
    case 'set-snapshots':
      return ['playwright-scripts/snapshots', { snapshots: action.snapshots }];
  }
}
