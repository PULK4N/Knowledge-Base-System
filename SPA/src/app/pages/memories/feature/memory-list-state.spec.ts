import { convertToParamMap } from '@angular/router';
import {
  DEFAULT_MEMORY_SEARCH_REQUEST,
  memorySearchQueryParams,
  parseMemorySearchRequest,
} from './memory-list-state';

describe('Memory list route state', () => {
  it('parses search, filters, paging, and sorting from the URL', () => {
    expect(
      parseMemorySearchRequest(
        convertToParamMap({
          page: '2',
          pageSize: '10',
          search: ' event sourcing ',
          hasSummary: 'true',
          minimumPromptCount: '4',
          sortBy: 'PromptCount',
          sortDirection: 'Ascending',
        }),
      ),
    ).toEqual({
      page: 2,
      pageSize: 10,
      search: 'event sourcing',
      semanticSearch: '',
      hasSummary: true,
      minimumPromptCount: 4,
      sortBy: 'PromptCount',
      sortDirection: 'Ascending',
    });
  });

  it('makes semantic search exclusive and defaults it to relevance', () => {
    expect(
      parseMemorySearchRequest(
        convertToParamMap({
          search: 'literal text',
          semanticSearch: 'what did we decide about the outbox?',
        }),
      ),
    ).toEqual({
      ...DEFAULT_MEMORY_SEARCH_REQUEST,
      search: '',
      semanticSearch: 'what did we decide about the outbox?',
      sortBy: 'Relevance',
    });
  });

  it('falls back to safe normal-search defaults', () => {
    expect(
      parseMemorySearchRequest(
        convertToParamMap({
          page: '-1',
          pageSize: '100',
          hasSummary: 'sometimes',
          minimumPromptCount: '-2',
          sortBy: 'Relevance',
          sortDirection: 'Sideways',
        }),
      ),
    ).toEqual(DEFAULT_MEMORY_SEARCH_REQUEST);
  });

  it('serializes only one search mode into a canonical URL', () => {
    expect(
      memorySearchQueryParams({
        page: 3,
        pageSize: 25,
        search: 'literal text',
        semanticSearch: 'semantic meaning',
        hasSummary: false,
        minimumPromptCount: 2,
        sortBy: 'PromptCount',
        sortDirection: 'Ascending',
      }),
    ).toEqual({
      page: 3,
      pageSize: 25,
      search: null,
      semanticSearch: 'semantic meaning',
      hasSummary: 'false',
      minimumPromptCount: 2,
      sortBy: 'PromptCount',
      sortDirection: 'Ascending',
    });
  });

  it('omits normal-mode defaults from the URL', () => {
    expect(memorySearchQueryParams(DEFAULT_MEMORY_SEARCH_REQUEST)).toEqual({
      page: null,
      pageSize: null,
      search: null,
      semanticSearch: null,
      hasSummary: null,
      minimumPromptCount: null,
      sortBy: null,
      sortDirection: null,
    });
  });
});
