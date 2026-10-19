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
          model: ' claude-opus-5-5 ',
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
      model: 'claude-opus-5-5',
      hasSummary: true,
      minimumPromptCount: 4,
      sortBy: 'PromptCount',
      sortDirection: 'Ascending',
    });
  });

  it.each([
    [{ search: 'outbox retry' }],
    [{ semanticSearch: 'what did we decide about the outbox?' }],
    [
      {
        search: 'outbox retry',
        semanticSearch: 'what did we decide about the outbox?',
      },
    ],
  ])('keeps text and vector search together and sorts by relevance: %j', params => {
    expect(parseMemorySearchRequest(convertToParamMap(params))).toEqual({
      ...DEFAULT_MEMORY_SEARCH_REQUEST,
      ...params,
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

  it('serializes both searches and the model into a canonical URL', () => {
    expect(
      memorySearchQueryParams({
        page: 3,
        pageSize: 25,
        search: 'literal text',
        semanticSearch: 'semantic meaning',
        model: 'gpt-5.5-codex',
        hasSummary: false,
        minimumPromptCount: 2,
        sortBy: 'PromptCount',
        sortDirection: 'Ascending',
      }),
    ).toEqual({
      page: 3,
      pageSize: 25,
      search: 'literal text',
      semanticSearch: 'semantic meaning',
      model: 'gpt-5.5-codex',
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
      model: null,
      hasSummary: null,
      minimumPromptCount: null,
      sortBy: null,
      sortDirection: null,
    });
  });
});
