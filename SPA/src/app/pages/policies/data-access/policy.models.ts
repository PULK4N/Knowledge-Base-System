import { PagedResult } from '../../../core/store/entity-store.service';

export interface Policy {
  readonly id: string;
  readonly title: string;
  readonly description: string;
}

export interface PolicyMemoryHistoryEntry {
  readonly eventName: string;
  readonly timestamp: string;
  readonly memoryId: string;
  readonly isUserOriginated: boolean;
}

export interface PolicyWithHistory extends Policy {
  readonly memoryHistory: readonly PolicyMemoryHistoryEntry[];
}

export interface PolicyTopicSummary {
  readonly id: string;
  readonly name: string;
  readonly description: string;
  readonly policyCount: number;
}

export interface PolicyAgentFamilySummary {
  readonly id: string;
  readonly name: string;
  readonly description: string;
  readonly policyCount: number;
}

export interface PolicyProjectSummary {
  readonly id: string;
  readonly name: string;
  readonly repositoryPaths: readonly string[];
}

export type ReviewLevel = 'Easy' | 'Medium' | 'Heavy';

export const REVIEW_LEVELS: readonly ReviewLevel[] = ['Easy', 'Medium', 'Heavy'];

export interface ReviewModel {
  readonly model: string;
  readonly reasoning: string;
}

/** Levels without a model are left out. */
export type ReviewLevelModels = Readonly<Partial<Record<ReviewLevel, ReviewModel>>>;

export interface ProjectSkillReview {
  readonly usedForReview: boolean;
  readonly reviewNotes: string;
  readonly reviewLevel: ReviewLevel | null;
}

export interface ProjectSkill extends ProjectSkillReview {
  readonly skillId: string;
  readonly name: string;
  readonly useScenarios: readonly string[];
}

export interface PolicyProjectDetails extends PolicyProjectSummary {
  readonly description: string;
  readonly topicNames: readonly string[];
  readonly skills: readonly ProjectSkill[];
  readonly reviewLevelModels: ReviewLevelModels;
}

export type PolicyScope =
  | { readonly kind: 'general' }
  | { readonly kind: 'topic'; readonly topicName: string }
  | { readonly kind: 'agentFamily'; readonly agentFamilyName: string }
  | { readonly kind: 'project'; readonly projectId: string };

export interface PolicySearchRequest {
  readonly page: number;
  readonly pageSize: number;
  readonly search: string;
}

export type PolicySearchResult = PagedResult<Policy>;
export type PolicyTopicSearchResult = PagedResult<PolicyTopicSummary>;
export type PolicyAgentFamilySearchResult =
  PagedResult<PolicyAgentFamilySummary>;
export type PolicyProjectSearchResult = PagedResult<PolicyProjectSummary>;

export interface PolicyDto {
  readonly policyId: string;
  readonly title: string;
  readonly description: string;
}

export interface PolicyHistoryDto extends PolicyDto {
  readonly memoryHistory: readonly PolicyMemoryHistoryEntry[];
}

export interface PolicyTopicSummaryDto {
  readonly topicName: string;
  readonly description: string;
  readonly policyCount: number;
}

export interface PolicyAgentFamilySummaryDto {
  readonly agentFamilyName: string;
  readonly description: string;
  readonly policyCount: number;
}

export interface PolicyProjectSummaryDto {
  readonly projectId: string;
  readonly projectName: string;
  readonly repositoryPaths: readonly string[];
}

export interface PolicyProjectDetailsDto extends PolicyProjectSummaryDto {
  readonly projectDescription: string;
  readonly topicNames: readonly string[];
  readonly skills: readonly ProjectSkill[];
  readonly reviewLevelModels: ReviewLevelModels;
}

export interface PolicyCommandResult {
  readonly status: string;
}

export interface PolicyAddedCommandResult extends PolicyCommandResult {
  readonly policyId: string;
}

export interface ProjectCreatedCommandResult extends PolicyCommandResult {
  readonly projectId: string;
}

export interface AddPolicyRequest {
  readonly title: string;
  readonly description: string;
}

export interface CreateTopicRequest {
  readonly topicName: string;
  readonly description: string;
}

export interface CreateAgentFamilyRequest {
  readonly agentFamilyName: string;
  readonly description: string;
}

export interface CreateProjectRequest {
  readonly projectName: string;
  readonly projectDescription: string;
  readonly repositoryPaths: readonly string[];
}

export interface UpdatePolicyRequest {
  readonly policyId: string;
  readonly title: string;
  readonly description: string;
}
