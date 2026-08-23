import { PagedResult } from '../../../core/store/entity-store.service';
import { ListSortDirection } from '../../../shared/list-state/list-state';

export interface SkillSummary {
  readonly id: string;
  readonly name: string;
}

export interface SkillListItem extends SkillSummary {
  readonly description: string;
  readonly tags: readonly string[];
  readonly referenceCount: number;
  readonly attachmentCount: number;
}

export interface SkillReference {
  readonly content: string;
  readonly loadAutomatically: boolean;
}

export interface SkillAttachment {
  readonly id: string;
  readonly name: string;
  readonly size: number;
  readonly fileType: string;
  readonly extension: string;
}

export interface Skill extends SkillListItem {
  readonly isDeleted: boolean;
  readonly content: string;
  readonly references: Readonly<Record<string, SkillReference>>;
  readonly attachments: Readonly<Record<string, SkillAttachment>>;
}

export type SkillSearchSortField =
  | 'Name'
  | 'ReferenceCount'
  | 'AttachmentCount';

export interface SkillSearchRequest {
  readonly page: number;
  readonly pageSize: number;
  readonly search: string;
  readonly tag: string;
  readonly hasReferences: boolean | null;
  readonly hasAttachments: boolean | null;
  readonly sortBy: SkillSearchSortField;
  readonly sortDirection: ListSortDirection;
}

export type SkillSearchResult = PagedResult<SkillListItem>;

export interface SkillListItemDto {
  readonly skillId: string;
  readonly name: string;
  readonly description: string;
  readonly tags: readonly string[];
  readonly referenceCount: number;
  readonly attachmentCount: number;
}

export interface SkillDto {
  readonly id: string;
  readonly isDeleted: boolean;
  readonly name: string;
  readonly description: string;
  readonly content: string;
  readonly tags: readonly string[];
  readonly references: Readonly<Record<string, SkillReference>>;
  readonly attachments: Readonly<Record<string, SkillAttachment>>;
}

export interface UpdateSkillRequest {
  readonly name: string;
  readonly description: string;
  readonly content: string;
  readonly tags: readonly string[];
}

export type AddSkillRequest = UpdateSkillRequest;

export interface SkillCreatedCommandResult extends SkillCommandResult {
  readonly skillId: string;
}

export interface UpdateSkillReferenceRequest {
  readonly relativePath: string;
  readonly content: string;
  readonly loadAutomatically: boolean;
}

export type AddSkillReferenceRequest = UpdateSkillReferenceRequest;

export interface SkillCommandResult {
  readonly status: string;
}
