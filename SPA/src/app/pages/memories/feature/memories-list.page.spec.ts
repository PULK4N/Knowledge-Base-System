import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  ActivatedRoute,
  ParamMap,
  Router,
  convertToParamMap,
} from '@angular/router';
import { BehaviorSubject, Observable, of } from 'rxjs';
import {
  MemorySearchRequest,
  MemorySearchResult,
} from '../data-access/memory.models';
import { MemoryService } from '../data-access/memory.service';
import { DEFAULT_MEMORY_SEARCH_REQUEST } from './memory-list-state';
import { memoryTitle } from './memory-title';
import { MemoriesListPage } from './memories-list.page';

describe('memoryTitle', () => {
  it.each([
    ['Fix session title', '# Event sourcing review', 'Fix session title'],
    ['  ', '# Event sourcing review\nMore details', 'Event sourcing review'],
    ['', '- Prefer focused tests', 'Prefer focused tests'],
    ['', '  \n\n', 'Conversation memory'],
  ])(
    'prefers the session title %j, then the summary %j',
    (sessionTitle, summary, expected) => {
      expect(memoryTitle({ sessionTitle, summary })).toBe(expected);
    },
  );

  it.each([
    ['A'.repeat(140), ''],
    ['', 'A'.repeat(140)],
  ])('shortens very long titles', (sessionTitle, summary) => {
    expect(memoryTitle({ sessionTitle, summary })).toBe(
      `${'A'.repeat(110)}…`,
    );
  });
});

describe('MemoriesListPage', () => {
  let fixture: ComponentFixture<MemoriesListPage>;
  let params: BehaviorSubject<ParamMap>;
  let router: { navigate: ReturnType<typeof vi.fn> };
  let cancelledRequests: number[];
  let requestNumber: number;

  beforeEach(() => {
    params = new BehaviorSubject(convertToParamMap({ search: 'first' }));
    router = { navigate: vi.fn() };
    cancelledRequests = [];
    requestNumber = 0;

    TestBed.configureTestingModule({
      imports: [MemoriesListPage],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: { queryParamMap: params.asObservable() },
        },
        { provide: Router, useValue: router },
        {
          provide: MemoryService,
          useValue: {
            search: vi.fn(
              () =>
                new Observable<MemorySearchResult>(() => {
                  const currentRequest = ++requestNumber;
                  return () => cancelledRequests.push(currentRequest);
                }),
            ),
            models: vi.fn(() => of(['claude-opus-5-5'])),
          },
        },
      ],
    });
    TestBed.overrideComponent(MemoriesListPage, { set: { template: '' } });
    fixture = TestBed.createComponent(MemoriesListPage);
  });

  it('cancels stale list reads when route-backed state changes', () => {
    const page = fixture.componentInstance as unknown as {
      readonly vm$: Observable<unknown>;
    };
    const subscription = page.vm$.subscribe();

    expect(requestNumber).toBe(1);
    params.next(
      convertToParamMap({
        semanticSearch: 'the replay decision',
        sortBy: 'Relevance',
      }),
    );

    expect(requestNumber).toBe(2);
    expect(cancelledRequests).toEqual([1]);
    subscription.unsubscribe();
  });

  it('keeps the other search input and sorts by relevance once searching', () => {
    const page = fixture.componentInstance as unknown as {
      search(request: MemorySearchRequest, value: string): void;
      semanticSearch(request: MemorySearchRequest, value: string): void;
    };
    const queryParams = (changes: Record<string, unknown>) => ({
      page: null,
      pageSize: null,
      search: null,
      semanticSearch: null,
      model: null,
      hasSummary: null,
      minimumPromptCount: null,
      sortBy: null,
      sortDirection: null,
      ...changes,
    });
    const searching: MemorySearchRequest = {
      ...DEFAULT_MEMORY_SEARCH_REQUEST,
      semanticSearch: 'the replay decision',
      sortBy: 'PromptCount',
      sortDirection: 'Ascending',
    };

    page.semanticSearch(DEFAULT_MEMORY_SEARCH_REQUEST, 'the replay decision');
    expect(router.navigate).toHaveBeenLastCalledWith([], {
      relativeTo: TestBed.inject(ActivatedRoute),
      queryParams: queryParams({ semanticSearch: 'the replay decision' }),
      replaceUrl: true,
    });

    page.search(searching, 'outbox');
    expect(router.navigate).toHaveBeenLastCalledWith([], {
      relativeTo: TestBed.inject(ActivatedRoute),
      queryParams: queryParams({
        search: 'outbox',
        semanticSearch: 'the replay decision',
        sortBy: 'PromptCount',
        sortDirection: 'Ascending',
      }),
      replaceUrl: true,
    });

    page.semanticSearch(searching, '');
    expect(router.navigate).toHaveBeenLastCalledWith([], {
      relativeTo: TestBed.inject(ActivatedRoute),
      queryParams: queryParams({}),
      replaceUrl: true,
    });
  });
});
