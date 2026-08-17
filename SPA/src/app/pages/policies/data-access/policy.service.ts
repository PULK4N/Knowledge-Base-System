import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import {
  Observable,
  concat,
  filter,
  ignoreElements,
  map,
  merge,
  of,
  switchMap,
  take,
  tap,
} from 'rxjs';
import {
  BaseEntity,
  EntityStore,
  PagedResult,
} from '../../../core/store/entity-store.service';
import {
  Policy,
  PolicyCommandResult,
  PolicyDto,
  PolicyProjectDetails,
  PolicyProjectDetailsDto,
  PolicyProjectSearchResult,
  PolicyProjectSummary,
  PolicyProjectSummaryDto,
  PolicyScope,
  PolicySearchRequest,
  PolicySearchResult,
  PolicyTopicSearchResult,
  PolicyTopicSummary,
  PolicyTopicSummaryDto,
  UpdatePolicyRequest,
} from './policy.models';

const TOPIC_ENTITY_TYPE = 'policy-topic';
const PROJECT_ENTITY_TYPE = 'policy-project';

function isProjectDetails(
  project: PolicyProjectSummary,
): project is PolicyProjectDetails {
  return 'topicNames' in project && 'description' in project;
}

export function policyControllerPath(scope: PolicyScope): string {
  switch (scope.kind) {
    case 'general':
      return '/api/policies/general';
    case 'topic':
      return `/api/policies/topics/${encodeURIComponent(scope.topicName)}/policies`;
    case 'project':
      return `/api/policies/projects/${encodeURIComponent(scope.projectId)}/policies`;
  }
}

function policyEntityType(scope: PolicyScope): string {
  switch (scope.kind) {
    case 'general':
      return 'general-policy';
    case 'topic':
      return `topic-policy:${scope.topicName}`;
    case 'project':
      return `project-policy:${scope.projectId}`;
  }
}

@Injectable({ providedIn: 'root' })
export class PolicyService {
  private readonly http = inject(HttpClient);
  private readonly store = inject(EntityStore);

  searchPolicies(
    scope: PolicyScope,
    request: PolicySearchRequest,
  ): Observable<PolicySearchResult> {
    return this.cachedSearch(
      policyControllerPath(scope),
      policyEntityType(scope),
      request,
      (policy: PolicyDto): Policy => ({
        id: policy.policyId,
        title: policy.title,
        description: policy.description,
      }),
    );
  }

  searchTopics(
    request: PolicySearchRequest,
  ): Observable<PolicyTopicSearchResult> {
    return this.cachedSearch(
      '/api/policies/topics',
      TOPIC_ENTITY_TYPE,
      request,
      (topic: PolicyTopicSummaryDto): PolicyTopicSummary => ({
        id: topic.topicName,
        name: topic.topicName,
        description: topic.description,
        policyCount: topic.policyCount,
      }),
    );
  }

  searchProjects(
    request: PolicySearchRequest,
  ): Observable<PolicyProjectSearchResult> {
    return this.cachedSearch(
      '/api/policies/projects',
      PROJECT_ENTITY_TYPE,
      request,
      (project: PolicyProjectSummaryDto): PolicyProjectSummary => ({
        id: project.projectId,
        name: project.projectName,
        repositoryPaths: project.repositoryPaths,
      }),
    );
  }

  watchProject(projectId: string): Observable<PolicyProjectDetails> {
    const refresh$ = this.http
      .get<PolicyProjectDetailsDto>(
        `/api/policies/projects/${encodeURIComponent(projectId)}`,
      )
      .pipe(
        map(
          (project): PolicyProjectDetails => ({
            id: project.projectId,
            name: project.projectName,
            description: project.projectDescription,
            repositoryPaths: project.repositoryPaths,
            topicNames: project.topicNames,
          }),
        ),
        tap(project => this.store.upsert(PROJECT_ENTITY_TYPE, project)),
      );

    return this.store
      .entity$<PolicyProjectSummary>(PROJECT_ENTITY_TYPE, projectId)
      .pipe(
        take(1),
        switchMap(cached =>
          cached && isProjectDetails(cached)
            ? concat(of(cached), refresh$)
            : refresh$,
        ),
      );
  }

  updatePolicy(
    scope: PolicyScope,
    request: UpdatePolicyRequest,
  ): Observable<PolicyCommandResult> {
    return this.http
      .post<PolicyCommandResult>(this.mutationPath(scope, 'update'), {
        ...this.scopeIdentity(scope),
        ...request,
      })
      .pipe(
        tap(() =>
          this.store.upsert(policyEntityType(scope), {
            id: request.policyId,
            title: request.title,
            description: request.description,
          } satisfies Policy),
        ),
      );
  }

  removePolicy(
    scope: PolicyScope,
    policyId: string,
  ): Observable<PolicyCommandResult> {
    return this.http
      .post<PolicyCommandResult>(this.mutationPath(scope, 'remove'), {
        ...this.scopeIdentity(scope),
        policyId,
      })
      .pipe(
        tap(() => this.store.remove(policyEntityType(scope), policyId)),
      );
  }

  private cachedSearch<TDto, TEntity extends BaseEntity>(
    path: string,
    entityType: string,
    request: PolicySearchRequest,
    mapItem: (item: TDto) => TEntity,
  ): Observable<PagedResult<TEntity>> {
    const normalizedSearch = request.search.trim();
    const queryKey = [
      entityType.toLowerCase(),
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
      .get<PagedResult<TDto>>(path, { params })
      .pipe(
        map(result => ({
          ...result,
          items: result.items.map(mapItem),
        })),
        tap(result =>
          this.store.replaceSearch(queryKey, entityType, result),
        ),
        ignoreElements(),
      );

    const cached$ = this.store.search$<TEntity>(queryKey).pipe(
      filter(
        (result): result is PagedResult<TEntity> => result !== undefined,
      ),
    );

    return merge(cached$, refresh$);
  }

  private mutationPath(
    scope: PolicyScope,
    action: 'update' | 'remove',
  ): string {
    switch (scope.kind) {
      case 'general':
        return `/api/policies/general/${action}`;
      case 'topic':
        return `/api/policies/topics/policies/${action}`;
      case 'project':
        return `/api/policies/projects/policies/${action}`;
    }
  }

  private scopeIdentity(scope: PolicyScope): Readonly<Record<string, string>> {
    switch (scope.kind) {
      case 'general':
        return {};
      case 'topic':
        return { topicName: scope.topicName };
      case 'project':
        return { projectId: scope.projectId };
    }
  }
}
