import { PagedResult } from '../../../core/store/entity-store.service';

export interface MemorySummary {
  readonly id: string;
  readonly threadId: string;
  readonly summary: string;
  readonly promptCount: number;
  readonly firstPromptTimestamp: string | null;
  readonly lastPromptTimestamp: string | null;
  readonly summaryTimestamp: string | null;
  readonly lastActivityTimestamp: string;
}

export interface MemorySearchRequest {
  readonly page: number;
  readonly pageSize: number;
  readonly search: string;
}

export type MemorySearchResult = PagedResult<MemorySummary>;

export interface MemorySummaryDto {
  readonly memoryId: string;
  readonly threadId: string;
  readonly summary: string;
  readonly promptCount: number;
  readonly firstPromptTimestamp: string | null;
  readonly lastPromptTimestamp: string | null;
  readonly summaryTimestamp: string | null;
  readonly lastActivityTimestamp: string;
}
