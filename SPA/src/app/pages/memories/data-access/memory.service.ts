import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, concat, map, of, switchMap, take, tap } from 'rxjs';
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
    const queryKey = [
      MEMORY_ENTITY_TYPE,
      request.page,
      request.pageSize,
      normalizedSearch.toLowerCase(),
    ].join(':');
    let params = new HttpParams()
      .set('page', request.page)
      .set('pageSize', request.pageSize);

    if (normalizedSearch) {
      params = params.set('search', normalizedSearch);
    }

    const refresh$ = this.http
      .get<PagedResult<MemorySummaryDto>>(this.controllerPath, { params })
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
      );

    return this.store.search$<MemorySummary>(queryKey).pipe(
      take(1),
      switchMap(cached => (cached ? concat(of(cached), refresh$) : refresh$)),
    );
  }
}
