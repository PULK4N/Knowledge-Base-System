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
  FeatureRecordContentRequest,
  FeatureRecordCreatedCommandResult,
  UpdateFeatureRecordRequest,
} from './feature.models';

const FEATURE_ENTITY_TYPE = 'feature';

function isFeature(entity: FeatureSummary | undefined): entity is Feature {
  return !!entity && 'records' in entity && 'plans' in entity;
}

@Injectable({ providedIn: 'root' })
export class FeatureService {
  private readonly http = inject(HttpClient);
  private readonly store = inject(EntityStore);
  private readonly controllerPath = '/api/features';

  search(request: FeatureSearchRequest): Observable<FeatureSearchResult> {
    const normalizedSearch = request.search.trim();
    const queryKey = [
      FEATURE_ENTITY_TYPE,
      request.page,
      request.pageSize,
      normalizedSearch.toLowerCase(),
    ].join(':');
    let params = new HttpParams()
      .set('page', request.page)
      .set('pageSize', request.pageSize);

    if (normalizedSearch) {
      params = params.set('search', normalizedSearch);
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
            currentPlanId: item.currentPlanId,
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

  updateStatus(id: string, status: string): Observable<Feature> {
    return this.postAndRefresh(id, 'status', { status });
  }

  addSkill(id: string, skillId: string): Observable<Feature> {
    return this.postAndRefresh(id, 'skills', { skillId });
  }

  removeSkill(id: string, skillId: string): Observable<Feature> {
    return this.postAndRefresh(id, 'skills/remove', { skillId });
  }

  addRecord(
    id: string,
    request: FeatureRecordContentRequest,
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

  addPlan(
    id: string,
    request: FeaturePlanContentRequest,
  ): Observable<Feature> {
    return this.http
      .post<FeaturePlanCreatedCommandResult>(
        `${this.featurePath(id)}/plans`,
        request,
      )
      .pipe(switchMap(() => this.refresh(id)));
  }

  updateCurrentPlan(
    id: string,
    request: FeaturePlanContentRequest,
  ): Observable<Feature> {
    return this.postAndRefresh(id, 'plans/current', request);
  }

  changeCurrentPlan(id: string, planId: string): Observable<Feature> {
    return this.postAndRefresh(id, 'plans/current/change', { planId });
  }

  removePlan(id: string, planId: string): Observable<Feature> {
    return this.postAndRefresh(id, 'plans/remove', { planId });
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
          planCount: feature.plans.length,
          recordCount: feature.records.length,
        })),
        tap(feature => this.store.upsert(FEATURE_ENTITY_TYPE, feature)),
      );
  }
}
