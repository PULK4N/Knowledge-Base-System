import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import {
  Observable,
  filter,
  ignoreElements,
  map,
  merge,
  shareReplay,
  tap,
} from 'rxjs';
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

// Matches the API's keyword limits for the full-text search.
const MAXIMUM_SEARCH_KEYWORDS = 20;
const MAXIMUM_SEARCH_KEYWORD_LENGTH = 100;

/** Splits the text search into the words a prompt chunk must all contain. */
export function memorySearchKeywords(search: string): string[] {
  return search
    .split(/\s+/)
    .filter(keyword => keyword.length > 0)
    .slice(0, MAXIMUM_SEARCH_KEYWORDS)
    .map(keyword => keyword.slice(0, MAXIMUM_SEARCH_KEYWORD_LENGTH));
}

const MEMORY_ENTITY_TYPE = 'memory';
const MEMORY_TOOL_CALL_ENTITY_TYPE = 'memoryToolCall';

@Injectable({ providedIn: 'root' })
export class MemoryService {
  private readonly http = inject(HttpClient);
  private readonly store = inject(EntityStore);
  private readonly controllerPath = '/api/memories';

  // The API caches the model list for hours, so one read per app session is
  // enough. An error is not replayed, so the next subscriber retries.
  private readonly models$ = this.http
    .get<string[]>(`${this.controllerPath}/models`)
    .pipe(shareReplay({ bufferSize: 1, refCount: false }));

  models(): Observable<readonly string[]> {
    return this.models$;
  }

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
    const keywords = memorySearchKeywords(request.search);
    const normalizedSemanticSearch = request.semanticSearch.trim();
    const model = request.model.trim();
    const isSearching = keywords.length > 0 || normalizedSemanticSearch !== '';
    const queryKey = JSON.stringify({
      entityType: MEMORY_ENTITY_TYPE,
      page: request.page,
      pageSize: request.pageSize,
      keywords: keywords.map(keyword => keyword.toLowerCase()),
      semanticSearch: normalizedSemanticSearch.toLowerCase(),
      model,
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

    if (normalizedSemanticSearch) {
      params = params.set('query', normalizedSemanticSearch);
    }

    for (const keyword of keywords) {
      params = params.append('keywords', keyword);
    }

    if (model) {
      params = params.set('model', model);
    }

    if (request.hasSummary !== null) {
      params = params.set('hasSummary', request.hasSummary);
    }

    if (request.minimumPromptCount !== null) {
      params = params.set('minimumPromptCount', request.minimumPromptCount);
    }

    const path = isSearching
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
              models: memory.models ?? [],
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
