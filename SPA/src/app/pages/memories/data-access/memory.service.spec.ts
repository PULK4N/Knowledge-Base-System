import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { MemoryService } from './memory.service';

describe('MemoryService', () => {
  let service: MemoryService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(MemoryService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('searches memories and maps the API identity', async () => {
    const resultPromise = firstValueFrom(
      service.search({
        page: 2,
        pageSize: 5,
        search: 'event sourcing',
        semanticSearch: '',
        hasSummary: true,
        minimumPromptCount: 3,
        sortBy: 'PromptCount',
        sortDirection: 'Ascending',
      }),
    );
    const request = http.expectOne(
      candidate =>
        candidate.url === '/api/memories' &&
        candidate.params.get('page') === '2' &&
        candidate.params.get('pageSize') === '5' &&
        candidate.params.get('search') === 'event sourcing' &&
        candidate.params.get('hasSummary') === 'true' &&
        candidate.params.get('minimumPromptCount') === '3' &&
        candidate.params.get('sortBy') === 'PromptCount' &&
        candidate.params.get('sortDirection') === 'Ascending',
    );

    request.flush({
      items: [
        {
          memoryId: 'memory-1',
          threadId: 'thread-1',
          summary: 'Implemented replay-safe event payloads.',
          promptCount: 8,
          firstPromptTimestamp: '2026-08-01T08:00:00Z',
          lastPromptTimestamp: '2026-08-01T09:00:00Z',
          summaryTimestamp: '2026-08-01T09:05:00Z',
          lastActivityTimestamp: '2026-08-01T09:05:00Z',
        },
      ],
      page: 2,
      pageSize: 5,
      totalCount: 6,
      totalPages: 2,
      hasPreviousPage: true,
      hasNextPage: false,
    });

    const result = await resultPromise;
    expect(result.items[0]).toEqual({
      id: 'memory-1',
      threadId: 'thread-1',
      summary: 'Implemented replay-safe event payloads.',
      promptCount: 8,
      firstPromptTimestamp: '2026-08-01T08:00:00Z',
      lastPromptTimestamp: '2026-08-01T09:00:00Z',
      summaryTimestamp: '2026-08-01T09:05:00Z',
      lastActivityTimestamp: '2026-08-01T09:05:00Z',
    });
  });

  it('uses the paged hybrid endpoint for semantic search', async () => {
    const resultPromise = firstValueFrom(
      service.search({
        page: 1,
        pageSize: 10,
        search: '',
        semanticSearch: 'what did we decide about event replay?',
        hasSummary: null,
        minimumPromptCount: null,
        sortBy: 'Relevance',
        sortDirection: 'Descending',
      }),
    );
    const request = http.expectOne(
      candidate =>
        candidate.url === '/api/memories/hybrid-search' &&
        candidate.params.get('query') ===
          'what did we decide about event replay?' &&
        candidate.params.get('sortBy') === 'Relevance',
    );

    request.flush({
      items: [],
      page: 1,
      pageSize: 10,
      totalCount: 0,
      totalPages: 0,
      hasPreviousPage: false,
      hasNextPage: false,
    });

    expect((await resultPromise).items).toEqual([]);
  });

  it('loads a conversation and normalizes message roles and payloads', async () => {
    const resultPromise = firstValueFrom(
      service.getConversation('memory-1'),
    );
    const request = http.expectOne(
      '/api/memories/memory-1/conversation',
    );

    expect(request.request.method).toBe('GET');
    request.flush({
      memoryId: 'memory-1',
      threadId: '33333333-3333-3333-3333-333333333333',
      summary: 'The chat refactored the outbox.',
      summaryTimestamp: '2026-08-22T12:00:00Z',
      firstPromptTimestamp: '2026-08-22T10:00:00Z',
      lastPromptTimestamp: '2026-08-22T11:00:00Z',
      messages: [
        {
          promptId: 'prompt-1',
          hookIndex: 0,
          timestamp: '2026-08-22T10:00:00Z',
          hookEventName: 'UserPromptSubmit',
          role: 'user',
          message: 'Refactor the outbox',
          payloadJson: '{"session_id":"019f"}',
        },
        {
          promptId: 'prompt-2',
          hookIndex: 0,
          timestamp: '2026-08-22T11:00:00Z',
          hookEventName: 'SessionStart',
          role: 'unknown-role',
          message: '',
          payloadJson: 'not json',
        },
      ],
    });

    const conversation = await resultPromise;
    expect(conversation.messages[0].id).toBe('prompt-1:0');
    expect(conversation.messages[0].role).toBe('user');
    expect(conversation.messages[0].payloadJson).toBe(
      '{\n  "session_id": "019f"\n}',
    );
    expect(conversation.messages[1].role).toBe('hook');
    expect(conversation.messages[1].payloadJson).toBe('not json');
  });
});
