import { PagedResult } from '../../../core/store/entity-store.service';

export interface SkillSummary {
  readonly id: string;
  readonly name: string;
}

export interface SkillReference {
  readonly content: string;
}

export interface SkillAttachment {
  readonly id: string;
  readonly name: string;
  readonly size: number;
  readonly fileType: string;
  readonly extension: string;
}

export interface Skill extends SkillSummary {
  readonly isDeleted: boolean;
  readonly description: string;
  readonly content: string;
  readonly tags: readonly string[];
  readonly references: Readonly<Record<string, SkillReference>>;
  readonly attachments: Readonly<Record<string, SkillAttachment>>;
}

export interface SkillSearchRequest {
  readonly page: number;
  readonly pageSize: number;
  readonly search: string;
}

export type SkillSearchResult = PagedResult<SkillSummary>;

export interface SkillSummaryDto {
  readonly skillId: string;
  readonly name: string;
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
