import { PagedResult } from '../../../core/store/entity-store.service';
import { ListSortDirection } from '../../../shared/list-state/list-state';

export type FeaturePlanContentType = 'Markdown' | 'Html';
export type FeaturePlanStatus =
  | 'NotStarted'
  | 'InProgress'
  | 'Delayed'
  | 'Completed';
export type FeatureRecordSourceType = 'Other' | 'Code' | 'Web' | 'Mcp';

/** Suggested record types; a type is free text stored in lower case. */
export const FEATURE_RECORD_TYPE_SUGGESTIONS: readonly string[] = [
  'conversation',
  'research',
];

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
  /** Free text in lower case, such as conversation or research. */
  readonly type: string;
  readonly title: string;
  readonly content: string;
  /** Null when the record has no source. */
  readonly sourceType: FeatureRecordSourceType | null;
  readonly sourceReference: string;
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

/** The feature branch when planId is null, otherwise a branch of another plan. */
export interface FeaturePlanBranchSource {
  readonly planId: string | null;
  readonly branchName: string;
}

export interface FeaturePlanBranch {
  readonly name: string;
  readonly description: string;
  readonly subBranches: readonly FeaturePlanBranch[];
  /**
   * Only on base branches: where the branch came from. Empty means the default,
   * the previous plan's first base branch or the feature branch.
   */
  readonly sources: readonly FeaturePlanBranchSource[];
}

/** One branch of the feature's branch graph; a branch can come from several. */
export interface FeatureBranchGraphNode {
  /** 'feature' for the feature branch, otherwise `<planId>:<branchName>`. */
  readonly key: string;
  readonly name: string | null;
  readonly description: string;
  readonly planId: string | null;
  readonly planTitle: string | null;
  readonly planStatus: FeaturePlanStatus | null;
  readonly isBaseBranch: boolean;
  readonly fromKeys: readonly string[];
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
  /** Base branches with nested sub-branches; the first is the main one. Empty only for old plans. */
  readonly branches: readonly FeaturePlanBranch[];
  /** Plan done before this one; its base branch is where this plan's base branch came from. */
  readonly previousPlanId: string | null;
  /** Records of the feature this plan links, in link order. */
  readonly recordIds: readonly string[];
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
  readonly plans: readonly FeaturePlan[];
  /** Null until the feature branch is set; features are often written before it exists. */
  readonly branch: FeatureBranch | null;
  /** Branch for Analyst checking all functionalities together before review finishes. */
  readonly compositeBranchName?: string | null;
  /**
   * The feature branch first, then the branches of plans not yet completed,
   * each after the branches it came from.
   */
  readonly branchGraph: readonly FeatureBranchGraphNode[];
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

export type FeatureSearchSortField =
  | 'UpdatedAt'
  | 'Name'
  | 'PlanCount'
  | 'RecordCount';

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
  readonly plans: readonly FeaturePlan[];
  readonly branch: FeatureBranch | null;
  readonly compositeBranchName?: string | null;
  readonly branchGraph: readonly FeatureBranchGraphNode[];
  readonly memoryHistory: readonly FeatureMemoryHistoryEntry[];
}

export interface AddFeatureRequest {
  readonly projectId: string;
  readonly name: string;
  readonly summary: string;
  readonly status: string;
  readonly compositeBranchName?: string;
}

export interface SetFeatureCompositeBranchRequest {
  /** Empty clears the composite testing branch. */
  readonly branchName: string;
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

export interface FeaturePlanCreatedCommandResult extends FeatureCommandResult {
  readonly planId: string;
}

export interface FeatureRecordContentRequest {
  readonly type: string;
  readonly title: string;
  readonly content: string;
  readonly sourceType: FeatureRecordSourceType | null;
  readonly sourceReference: string;
}

export interface AddFeatureRecordRequest extends FeatureRecordContentRequest {
  /** Plans that should link the new record; the link is stored on each plan. */
  readonly planIds: readonly string[];
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

export interface FeaturePlanContentRequest {
  readonly title: string;
  readonly content: string;
  readonly contentType: FeaturePlanContentType;
  readonly additionalInfo: string;
  readonly status: FeaturePlanStatus;
  /** Records to link. Null keeps the current links; an empty list removes them. */
  readonly recordIds?: readonly string[] | null;
  /** Only when completing: where each base branch was merged. */
  readonly mergedInto?: readonly FeaturePlanBranchMerge[];
}

export interface AddFeaturePlanRequest extends FeaturePlanContentRequest {
  readonly branchName: string;
  readonly branchDescription: string;
  readonly previousPlanId: string | null;
  /** Where the base branch came from; empty uses the previous plan or the feature branch. */
  readonly branchSources?: readonly FeaturePlanBranchSource[];
}

export interface SetFeatureBranchRequest {
  readonly branchName: string;
  readonly description: string;
}

export interface AddFeaturePlanBranchRequest {
  readonly branchName: string;
  readonly description: string;
  /** Null adds another base branch to the plan. */
  readonly parentBranchName: string | null;
  /** Only for a new base branch: where it came from. */
  readonly sources?: readonly FeaturePlanBranchSource[];
}

/**
 * Where a base branch of a completed plan was merged: the feature branch when
 * intoPlanId is null, otherwise a branch of another plan.
 */
export interface FeaturePlanBranchMerge {
  readonly branchName: string;
  readonly intoPlanId: string | null;
  readonly intoBranchName: string;
}

export interface SetFeaturePlanBranchSourcesRequest {
  readonly branchName: string;
  /** An empty list goes back to the default source. */
  readonly sources: readonly FeaturePlanBranchSource[];
}

export interface UpdateFeaturePlanBranchRequest {
  readonly branchName: string;
  readonly newBranchName: string;
  readonly description: string;
}
