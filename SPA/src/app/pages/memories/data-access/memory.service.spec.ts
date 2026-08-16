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
      service.search({ page: 2, pageSize: 5, search: 'event sourcing' }),
    );
    const request = http.expectOne(
      candidate =>
        candidate.url === '/api/memories' &&
        candidate.params.get('page') === '2' &&
        candidate.params.get('pageSize') === '5' &&
        candidate.params.get('search') === 'event sourcing',
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
});
