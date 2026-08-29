import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  ActivatedRoute,
  ParamMap,
  Router,
  convertToParamMap,
} from '@angular/router';
import { BehaviorSubject, Observable } from 'rxjs';
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
    ['# Event sourcing review\nMore details', 'Event sourcing review'],
    ['- Prefer focused tests', 'Prefer focused tests'],
    ['  \n\n', 'Conversation memory'],
  ])('derives a readable list title from %j', (summary, expected) => {
    expect(memoryTitle(summary)).toBe(expected);
  });

  it('shortens very long first lines', () => {
    expect(memoryTitle('A'.repeat(140))).toBe(`${'A'.repeat(110)}…`);
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

  it('clears the other search mode whenever one search receives input', () => {
    const page = fixture.componentInstance as unknown as {
      search(request: MemorySearchRequest, value: string): void;
      semanticSearch(request: MemorySearchRequest, value: string): void;
    };

    page.semanticSearch(DEFAULT_MEMORY_SEARCH_REQUEST, 'the replay decision');
    expect(router.navigate).toHaveBeenLastCalledWith([], {
      relativeTo: TestBed.inject(ActivatedRoute),
      queryParams: {
        page: null,
        pageSize: null,
        search: null,
        semanticSearch: 'the replay decision',
        hasSummary: null,
        minimumPromptCount: null,
        sortBy: null,
        sortDirection: null,
      },
      replaceUrl: true,
    });

    page.search(
      {
        ...DEFAULT_MEMORY_SEARCH_REQUEST,
        semanticSearch: 'the replay decision',
        sortBy: 'Relevance',
      },
      'literal summary',
    );
    expect(router.navigate).toHaveBeenLastCalledWith([], {
      relativeTo: TestBed.inject(ActivatedRoute),
      queryParams: {
        page: null,
        pageSize: null,
        search: 'literal summary',
        semanticSearch: null,
        hasSummary: null,
        minimumPromptCount: null,
        sortBy: null,
        sortDirection: null,
      },
      replaceUrl: true,
    });
  });
});
