import { PagedResult } from '../../../core/store/entity-store.service';
import { ListSortDirection } from '../../../shared/list-state/list-state';

export type FeaturePlanContentType = 'Markdown' | 'Html';
export type FeaturePlanStatus =
  | 'NotStarted'
  | 'InProgress'
  | 'Delayed'
  | 'Completed';
export type FeatureResearchDiscoverySourceType =
  | 'Other'
  | 'Code'
  | 'Web'
  | 'Mcp';

export interface FeatureSummary {
  readonly id: string;
  readonly projectId: string;
  readonly name: string;
  readonly summary: string;
  readonly status: string;
  readonly planCount: number;
  readonly recordCount: number;
}

export interface FeatureRecord {
  readonly id: string;
  readonly userMessage: string;
  readonly aiAnswer: string;
  readonly createdAt: string;
  readonly updatedAt: string;
}

export interface FeatureReviewNote {
  readonly id: string;
  readonly title: string;
  readonly content: string;
  readonly createdAt: string;
  readonly updatedAt: string;
}

export interface FeatureResearchDiscovery {
  readonly id: string;
  readonly title: string;
  readonly content: string;
  readonly sourceType: FeatureResearchDiscoverySourceType;
  readonly sourceReference: string;
  readonly createdAt: string;
  readonly updatedAt: string;
}

export interface FeaturePlanBranch {
  readonly name: string;
  readonly description: string;
  readonly subBranches: readonly FeaturePlanBranch[];
}

export interface FeatureBranch {
  readonly name: string;
  readonly description: string;
}

export interface FeaturePlan {
  readonly id: string;
  readonly title: string;
  readonly content: string;
  readonly contentType: FeaturePlanContentType;
  readonly additionalInfo: string;
  readonly status: FeaturePlanStatus;
  /** Base branch with nested sub-branches; null only for plans made before branches were required. */
  readonly branch: FeaturePlanBranch | null;
  /** Plan done before this one; its base branch is where this plan's base branch came from. */
  readonly previousPlanId: string | null;
  readonly createdAt: string;
  readonly updatedAt: string;
}

export interface FeatureMemoryHistoryEntry {
  readonly eventName: string;
  readonly timestamp: string;
  readonly memoryId: string;
  readonly isUserOriginated: boolean;
}

export interface Feature extends FeatureSummary {
  readonly isDeleted: boolean;
  readonly relatedSkillIds: readonly string[];
  readonly records: readonly FeatureRecord[];
  readonly reviewNotes: readonly FeatureReviewNote[];
  readonly researchDiscoveries: readonly FeatureResearchDiscovery[];
  readonly plans: readonly FeaturePlan[];
  /** Null until the feature branch is set; features are often written before it exists. */
  readonly branch: FeatureBranch | null;
  readonly memoryHistory: readonly FeatureMemoryHistoryEntry[];
}

export interface FeatureSearchRequest {
  readonly page: number;
  readonly pageSize: number;
  readonly search: string;
  readonly projectId: string;
  readonly sortBy: FeatureSearchSortField;
  readonly sortDirection: ListSortDirection;
}

export type FeatureSearchSortField = 'Name' | 'PlanCount' | 'RecordCount';

export type FeatureSearchResult = PagedResult<FeatureSummary>;

export interface FeatureSummaryDto {
  readonly featureId: string;
  readonly projectId: string;
  readonly name: string;
  readonly summary: string;
  readonly status: string;
  readonly planCount: number;
  readonly recordCount: number;
}

export interface FeatureDto {
  readonly id: string;
  readonly isDeleted: boolean;
  readonly projectId: string;
  readonly name: string;
  readonly summary: string;
  readonly status: string;
  readonly relatedSkillIds: readonly string[];
  readonly records: readonly FeatureRecord[];
  readonly reviewNotes: readonly FeatureReviewNote[];
  readonly researchDiscoveries: readonly FeatureResearchDiscovery[];
  readonly plans: readonly FeaturePlan[];
  readonly branch: FeatureBranch | null;
  readonly memoryHistory: readonly FeatureMemoryHistoryEntry[];
}

export interface AddFeatureRequest {
  readonly projectId: string;
  readonly name: string;
  readonly summary: string;
  readonly status: string;
}

export interface FeatureCreatedCommandResult {
  readonly status: string;
  readonly featureId: string;
}

export interface FeatureCommandResult {
  readonly status: string;
}

export interface FeatureRecordCreatedCommandResult extends FeatureCommandResult {
  readonly recordId: string;
}

export interface FeatureReviewNoteCreatedCommandResult
  extends FeatureCommandResult {
  readonly reviewNoteId: string;
}

export interface FeatureResearchDiscoveryCreatedCommandResult
  extends FeatureCommandResult {
  readonly discoveryId: string;
}

export interface FeaturePlanCreatedCommandResult extends FeatureCommandResult {
  readonly planId: string;
}

export interface FeatureRecordContentRequest {
  readonly userMessage: string;
  readonly aiAnswer: string;
}

export interface UpdateFeatureRecordRequest extends FeatureRecordContentRequest {
  readonly recordId: string;
}

export interface FeatureReviewNoteContentRequest {
  readonly title: string;
  readonly content: string;
}

export interface UpdateFeatureReviewNoteRequest
  extends FeatureReviewNoteContentRequest {
  readonly reviewNoteId: string;
}

export interface FeatureResearchDiscoveryContentRequest {
  readonly title: string;
  readonly content: string;
  readonly sourceType: FeatureResearchDiscoverySourceType;
  readonly sourceReference: string;
}

export interface UpdateFeatureResearchDiscoveryRequest
  extends FeatureResearchDiscoveryContentRequest {
  readonly discoveryId: string;
}

export interface FeaturePlanContentRequest {
  readonly title: string;
  readonly content: string;
  readonly contentType: FeaturePlanContentType;
  readonly additionalInfo: string;
  readonly status: FeaturePlanStatus;
}

export interface AddFeaturePlanRequest extends FeaturePlanContentRequest {
  readonly branchName: string;
  readonly branchDescription: string;
  /** Null when the base branch comes from the feature branch. */
  readonly previousPlanId: string | null;
}

export interface SetFeatureBranchRequest {
  readonly branchName: string;
  readonly description: string;
}

export interface AddFeaturePlanBranchRequest {
  readonly branchName: string;
  readonly description: string;
  /** Null only adds a base branch to a plan that has none. */
  readonly parentBranchName: string | null;
}

export interface UpdateFeaturePlanBranchRequest {
  readonly branchName: string;
  readonly newBranchName: string;
  readonly description: string;
}
