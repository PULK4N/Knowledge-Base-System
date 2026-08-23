import { PagedResult } from '../../../core/store/entity-store.service';
import { ListSortDirection } from '../../../shared/list-state/list-state';

export type FeaturePlanContentType = 'Markdown' | 'Html';
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
  readonly currentPlanId: string | null;
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

export interface FeatureResearchDiscovery {
  readonly id: string;
  readonly content: string;
  readonly sourceType: FeatureResearchDiscoverySourceType;
  readonly sourceReference: string;
  readonly createdAt: string;
  readonly updatedAt: string;
}

export interface FeaturePlan {
  readonly id: string;
  readonly title: string;
  readonly content: string;
  readonly contentType: FeaturePlanContentType;
  readonly createdAt: string;
  readonly updatedAt: string;
}

export interface Feature extends FeatureSummary {
  readonly isDeleted: boolean;
  readonly relatedSkillIds: readonly string[];
  readonly records: readonly FeatureRecord[];
  readonly researchDiscoveries: readonly FeatureResearchDiscovery[];
  readonly plans: readonly FeaturePlan[];
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
  readonly currentPlanId: string | null;
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
  readonly researchDiscoveries: readonly FeatureResearchDiscovery[];
  readonly plans: readonly FeaturePlan[];
  readonly currentPlanId: string | null;
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

export interface FeatureResearchDiscoveryContentRequest {
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
}
