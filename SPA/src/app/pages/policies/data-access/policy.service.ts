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
  BaseEntity,
  EntityStore,
  PagedResult,
} from '../../../core/store/entity-store.service';
import {
  AddPolicyRequest,
  CreateAgentFamilyRequest,
  CreateProjectRequest,
  CreateTopicRequest,
  Policy,
  PolicyAgentFamilySearchResult,
  PolicyAgentFamilySummary,
  PolicyAgentFamilySummaryDto,
  PolicyAddedCommandResult,
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
  ProjectCreatedCommandResult,
  UpdatePolicyRequest,
} from './policy.models';

const TOPIC_ENTITY_TYPE = 'policy-topic';
const AGENT_FAMILY_ENTITY_TYPE = 'policy-agent-family';
const PROJECT_ENTITY_TYPE = 'policy-project';

function isProjectDetails(
  project: PolicyProjectSummary | undefined,
): project is PolicyProjectDetails {
  return !!project && 'topicNames' in project && 'description' in project;
}

export function policyControllerPath(scope: PolicyScope): string {
  switch (scope.kind) {
    case 'general':
      return '/api/policies/general';
    case 'topic':
      return `/api/policies/topics/${encodeURIComponent(scope.topicName)}/policies`;
    case 'agentFamily':
      return `/api/policies/agent-families/${encodeURIComponent(scope.agentFamilyName)}/policies`;
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
    case 'agentFamily':
      return `agent-family-policy:${scope.agentFamilyName}`;
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

  searchAgentFamilies(
    request: PolicySearchRequest,
  ): Observable<PolicyAgentFamilySearchResult> {
    return this.cachedSearch(
      '/api/policies/agent-families',
      AGENT_FAMILY_ENTITY_TYPE,
      request,
      (
        agentFamily: PolicyAgentFamilySummaryDto,
      ): PolicyAgentFamilySummary => ({
        id: agentFamily.agentFamilyName,
        name: agentFamily.agentFamilyName,
        description: agentFamily.description,
        policyCount: agentFamily.policyCount,
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
    const refresh$ = this.refreshProject(projectId).pipe(ignoreElements());
    const cached$ = this.store
      .entity$<PolicyProjectSummary>(PROJECT_ENTITY_TYPE, projectId)
      .pipe(filter(isProjectDetails));

    return merge(cached$, refresh$);
  }

  addPolicy(
    scope: PolicyScope,
    request: AddPolicyRequest,
  ): Observable<Policy> {
    return this.http
      .post<PolicyAddedCommandResult>(this.addPolicyPath(scope), {
        ...this.scopeIdentity(scope),
        ...request,
      })
      .pipe(
        map(result => ({
          id: result.policyId,
          title: request.title,
          description: request.description,
        })),
        tap(policy => this.store.upsert(policyEntityType(scope), policy)),
      );
  }

  createTopic(request: CreateTopicRequest): Observable<PolicyTopicSummary> {
    return this.http
      .post<PolicyCommandResult>('/api/policies/topics', request)
      .pipe(
        map(() => ({
          id: request.topicName,
          name: request.topicName,
          description: request.description,
          policyCount: 0,
        })),
        tap(topic => this.store.upsert(TOPIC_ENTITY_TYPE, topic)),
      );
  }

  removeTopic(topicName: string): Observable<PolicyCommandResult> {
    return this.http
      .post<PolicyCommandResult>('/api/policies/topics/remove', {
        topicName,
      })
      .pipe(tap(() => this.store.remove(TOPIC_ENTITY_TYPE, topicName)));
  }

  createAgentFamily(
    request: CreateAgentFamilyRequest,
  ): Observable<PolicyAgentFamilySummary> {
    return this.http
      .post<PolicyCommandResult>('/api/policies/agent-families', request)
      .pipe(
        map(() => ({
          id: request.agentFamilyName,
          name: request.agentFamilyName,
          description: request.description,
          policyCount: 0,
        })),
        tap(agentFamily =>
          this.store.upsert(AGENT_FAMILY_ENTITY_TYPE, agentFamily),
        ),
      );
  }

  createProject(
    request: CreateProjectRequest,
  ): Observable<PolicyProjectDetails> {
    return this.http
      .post<ProjectCreatedCommandResult>('/api/policies/projects', request)
      .pipe(
        map(result => ({
          id: result.projectId,
          name: request.projectName,
          description: request.projectDescription,
          repositoryPaths: request.repositoryPaths,
          topicNames: [],
        })),
        tap(project => this.store.upsert(PROJECT_ENTITY_TYPE, project)),
      );
  }

  addProjectRepository(
    projectId: string,
    repositoryPath: string,
  ): Observable<PolicyProjectDetails> {
    return this.http
      .post<PolicyCommandResult>('/api/policies/projects/repositories', {
        projectId,
        repositoryPath,
      })
      .pipe(switchMap(() => this.refreshProject(projectId)));
  }

  addProjectTopic(
    projectId: string,
    topicName: string,
  ): Observable<PolicyProjectDetails> {
    return this.http
      .post<PolicyCommandResult>('/api/policies/projects/topics', {
        projectId,
        topicName,
      })
      .pipe(switchMap(() => this.refreshProject(projectId)));
  }

  removeProjectTopic(
    projectId: string,
    topicName: string,
  ): Observable<PolicyProjectDetails> {
    return this.http
      .post<PolicyCommandResult>('/api/policies/projects/topics/remove', {
        projectId,
        topicName,
      })
      .pipe(switchMap(() => this.refreshProject(projectId)));
  }

  private refreshProject(projectId: string): Observable<PolicyProjectDetails> {
    return this.http
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
      case 'agentFamily':
        return `/api/policies/agent-families/policies/${action}`;
      case 'project':
        return `/api/policies/projects/policies/${action}`;
    }
  }

  private addPolicyPath(scope: PolicyScope): string {
    switch (scope.kind) {
      case 'general':
        return '/api/policies/general';
      case 'topic':
        return '/api/policies/topics/policies';
      case 'agentFamily':
        return '/api/policies/agent-families/policies';
      case 'project':
        return '/api/policies/projects/policies';
    }
  }

  private scopeIdentity(scope: PolicyScope): Readonly<Record<string, string>> {
    switch (scope.kind) {
      case 'general':
        return {};
      case 'topic':
        return { topicName: scope.topicName };
      case 'agentFamily':
        return { agentFamilyName: scope.agentFamilyName };
      case 'project':
        return { projectId: scope.projectId };
    }
  }
}
