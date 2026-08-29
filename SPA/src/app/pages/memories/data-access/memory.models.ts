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

export type MemoryMessageRole = 'user' | 'assistant' | 'hook';

export interface MemoryConversationMessage {
  readonly id: string;
  readonly promptId: string;
  readonly hookIndex: number;
  readonly timestamp: string;
  readonly hookEventName: string;
  readonly role: MemoryMessageRole;
  readonly message: string;
  readonly payloadJson: string;
}

export interface MemoryConversation {
  readonly memoryId: string;
  readonly threadId: string;
  readonly summary: string;
  readonly summaryTimestamp: string | null;
  readonly firstPromptTimestamp: string | null;
  readonly lastPromptTimestamp: string | null;
  readonly messages: readonly MemoryConversationMessage[];
}

export interface MemoryConversationMessageDto {
  readonly promptId: string;
  readonly hookIndex: number;
  readonly timestamp: string;
  readonly hookEventName: string;
  readonly role: string;
  readonly message: string;
  readonly payloadJson: string;
}

export interface MemoryConversationDto {
  readonly memoryId: string;
  readonly threadId: string;
  readonly summary: string;
  readonly summaryTimestamp: string | null;
  readonly firstPromptTimestamp: string | null;
  readonly lastPromptTimestamp: string | null;
  readonly messages: readonly MemoryConversationMessageDto[];
}
