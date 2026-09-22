import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, filter, ignoreElements, map, merge, tap } from 'rxjs';
import {
  EntityStore,
  PagedResult,
} from '../../../core/store/entity-store.service';
import {
  MemoryConversation,
  MemoryConversationDto,
  MemoryConversationMessage,
  MemoryConversationMessageDto,
  MemoryMessageRole,
  MemorySearchRequest,
  MemorySearchResult,
  MemorySummary,
  MemorySummaryDto,
  MemoryToolCall,
  MemoryToolCallDto,
  MemoryToolCallSearchItem,
  MemoryToolCallSearchItemDto,
  MemoryToolCallSearchRequest,
  MemoryToolCallSearchResult,
} from './memory.models';

const MEMORY_MESSAGE_ROLES: readonly MemoryMessageRole[] = [
  'user',
  'assistant',
  'hook',
];

function formatJson(value: string): string {
  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
}

function toMessage(
  message: MemoryConversationMessageDto,
): MemoryConversationMessage {
  const role = MEMORY_MESSAGE_ROLES.find(
    candidate => candidate === message.role,
  );

  return {
    ...message,
    id: `${message.promptId}:${message.hookIndex}`,
    role: role ?? 'hook',
    payloadJson: formatJson(message.payloadJson),
  };
}

function toToolCallSearchItem(
  toolCall: MemoryToolCallSearchItemDto,
): MemoryToolCallSearchItem {
  return {
    ...toolCall,
    id: `${toolCall.memoryId}:${toolCall.promptId}:${toolCall.toolCallIndex}`,
    payloadJson: formatJson(toolCall.payloadJson),
  };
}

function toToolCall(toolCall: MemoryToolCallDto): MemoryToolCall {
  return {
    ...toolCall,
    id: `${toolCall.promptId}:${toolCall.toolCallIndex}`,
    payloadJson: formatJson(toolCall.payloadJson),
  };
}

const MEMORY_ENTITY_TYPE = 'memory';
const MEMORY_TOOL_CALL_ENTITY_TYPE = 'memoryToolCall';

@Injectable({ providedIn: 'root' })
export class MemoryService {
  private readonly http = inject(HttpClient);
  private readonly store = inject(EntityStore);
  private readonly controllerPath = '/api/memories';

  getConversation(memoryId: string): Observable<MemoryConversation> {
    return this.http
      .get<MemoryConversationDto>(
        `${this.controllerPath}/${encodeURIComponent(memoryId)}/conversation`,
      )
      .pipe(
        map(conversation => ({
          ...conversation,
          messages: conversation.messages.map(toMessage),
          toolCalls: (conversation.toolCalls ?? []).map(toToolCall),
        })),
      );
  }

  search(request: MemorySearchRequest): Observable<MemorySearchResult> {
    const normalizedSearch = request.search.trim();
    const normalizedSemanticSearch = request.semanticSearch.trim();
    const queryKey = JSON.stringify({
      entityType: MEMORY_ENTITY_TYPE,
      page: request.page,
      pageSize: request.pageSize,
      search: normalizedSearch.toLowerCase(),
      semanticSearch: normalizedSemanticSearch.toLowerCase(),
      hasSummary: request.hasSummary,
      minimumPromptCount: request.minimumPromptCount,
      sortBy: request.sortBy,
      sortDirection: request.sortDirection,
    });
    let params = new HttpParams()
      .set('page', request.page)
      .set('pageSize', request.pageSize)
      .set('sortBy', request.sortBy)
      .set('sortDirection', request.sortDirection);

    const activeSearch = normalizedSemanticSearch || normalizedSearch;
    if (activeSearch) {
      params = params.set(
        normalizedSemanticSearch ? 'query' : 'search',
        activeSearch,
      );
    }

    if (request.hasSummary !== null) {
      params = params.set('hasSummary', request.hasSummary);
    }

    if (request.minimumPromptCount !== null) {
      params = params.set('minimumPromptCount', request.minimumPromptCount);
    }

    const path = normalizedSemanticSearch
      ? `${this.controllerPath}/hybrid-search`
      : this.controllerPath;
    const refresh$ = this.http
      .get<PagedResult<MemorySummaryDto>>(path, { params })
      .pipe(
        map(result => ({
          ...result,
          items: result.items.map(
            (memory): MemorySummary => ({
              id: memory.memoryId,
              threadId: memory.threadId,
              sessionTitle: memory.sessionTitle,
              summary: memory.summary,
              promptCount: memory.promptCount,
              firstPromptTimestamp: memory.firstPromptTimestamp,
              lastPromptTimestamp: memory.lastPromptTimestamp,
              summaryTimestamp: memory.summaryTimestamp,
              lastActivityTimestamp: memory.lastActivityTimestamp,
            }),
          ),
        })),
        tap(result =>
          this.store.replaceSearch(queryKey, MEMORY_ENTITY_TYPE, result),
        ),
        ignoreElements(),
      );

    const cached$ = this.store.search$<MemorySummary>(queryKey).pipe(
      filter(
        (result): result is MemorySearchResult => result !== undefined,
      ),
    );

    return merge(cached$, refresh$);
  }

  searchToolCalls(
    request: MemoryToolCallSearchRequest,
  ): Observable<MemoryToolCallSearchResult> {
    const normalizedSearch = request.search.trim();
    const queryKey = JSON.stringify({
      entityType: MEMORY_TOOL_CALL_ENTITY_TYPE,
      page: request.page,
      pageSize: request.pageSize,
      search: normalizedSearch.toLowerCase(),
      toolName: request.toolName.trim().toLowerCase(),
      sortBy: request.sortBy,
      sortDirection: request.sortDirection,
    });
    let params = new HttpParams()
      .set('page', request.page)
      .set('pageSize', request.pageSize)
      .set('sortBy', request.sortBy)
      .set('sortDirection', request.sortDirection);

    if (normalizedSearch) {
      params = params.set('search', normalizedSearch);
    }

    const toolName = request.toolName.trim();
    if (toolName) {
      params = params.set('toolName', toolName);
    }

    const refresh$ = this.http
      .get<PagedResult<MemoryToolCallSearchItemDto>>(
        `${this.controllerPath}/tool-calls`,
        { params },
      )
      .pipe(
        map(result => ({
          ...result,
          items: result.items.map(toToolCallSearchItem),
        })),
        tap(result =>
          this.store.replaceSearch(
            queryKey,
            MEMORY_TOOL_CALL_ENTITY_TYPE,
            result,
          ),
        ),
        ignoreElements(),
      );

    const cached$ = this.store
      .search$<MemoryToolCallSearchItem>(queryKey)
      .pipe(
        filter(
          (result): result is MemoryToolCallSearchResult =>
            result !== undefined,
        ),
      );

    return merge(cached$, refresh$);
  }
}
