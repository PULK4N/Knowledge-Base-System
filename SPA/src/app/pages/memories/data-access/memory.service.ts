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

const MEMORY_ENTITY_TYPE = 'memory';

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
}
