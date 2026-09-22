import { convertToParamMap } from '@angular/router';
import {
  DEFAULT_MEMORY_TOOL_CALL_SEARCH_REQUEST,
  memoryToolCallSearchQueryParams,
  parseMemoryToolCallSearchRequest,
} from './memory-tool-call-list-state';

describe('Memory tool call route state', () => {
  it('parses search, paging, and sorting from the URL', () => {
    expect(
      parseMemoryToolCallSearchRequest(
        convertToParamMap({
          page: '3',
          pageSize: '10',
          search: '  command ls  ',
          toolName: '  Bash  ',
          sortBy: 'ToolName',
          sortDirection: 'Ascending',
        }),
      ),
    ).toEqual({
      page: 3,
      pageSize: 10,
      search: 'command ls',
      toolName: 'Bash',
      sortBy: 'ToolName',
      sortDirection: 'Ascending',
    });
  });

  it('falls back to safe defaults for unknown values', () => {
    expect(
      parseMemoryToolCallSearchRequest(
        convertToParamMap({
          page: '-1',
          pageSize: '7',
          sortBy: 'PayloadJson',
          sortDirection: 'Sideways',
        }),
      ),
    ).toEqual(DEFAULT_MEMORY_TOOL_CALL_SEARCH_REQUEST);
  });

  it('omits default criteria from the URL', () => {
    expect(
      memoryToolCallSearchQueryParams(DEFAULT_MEMORY_TOOL_CALL_SEARCH_REQUEST),
    ).toEqual({
      page: null,
      pageSize: null,
      search: null,
      toolName: null,
      sortBy: null,
      sortDirection: null,
    });
  });

  it('keeps criteria that differ from the defaults', () => {
    expect(
      memoryToolCallSearchQueryParams({
        page: 2,
        pageSize: 100,
        search: 'skill_get',
        toolName: 'Bash',
        sortBy: 'ToolName',
        sortDirection: 'Ascending',
      }),
    ).toEqual({
      page: 2,
      pageSize: 100,
      search: 'skill_get',
      toolName: 'Bash',
      sortBy: 'ToolName',
      sortDirection: 'Ascending',
    });
  });
});
