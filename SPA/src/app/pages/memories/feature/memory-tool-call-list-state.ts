import { ParamMap, Params } from '@angular/router';
import {
  ListQueryParams,
  omitDefault,
  omitEmpty,
  readAllowedInteger,
  readAllowedValue,
  readPositiveInteger,
} from '../../../shared/list-state/list-route-state';
import { ListSortDirection } from '../../../shared/list-state/list-state';
import {
  MemoryToolCallSearchRequest,
  MemoryToolCallSearchSortField,
} from '../data-access/memory.models';

export const MEMORY_TOOL_CALL_DEFAULT_PAGE_SIZE = 25;
export const MEMORY_TOOL_CALL_PAGE_SIZES = [10, 25, 100] as const;
const MAXIMUM_OFFSET = 100_000;
const MAXIMUM_SEARCH_LENGTH = 500;

const MEMORY_TOOL_CALL_SORT_FIELDS: readonly MemoryToolCallSearchSortField[] =
  ['Timestamp', 'ToolName'];
const SORT_DIRECTIONS: readonly ListSortDirection[] = [
  'Ascending',
  'Descending',
];

export const DEFAULT_MEMORY_TOOL_CALL_SEARCH_REQUEST: MemoryToolCallSearchRequest =
  {
    page: 1,
    pageSize: MEMORY_TOOL_CALL_DEFAULT_PAGE_SIZE,
    search: '',
    toolName: '',
    sortBy: 'Timestamp',
    sortDirection: 'Descending',
  };

export function parseMemoryToolCallSearchRequest(
  params: ParamMap,
): MemoryToolCallSearchRequest {
  const pageSize = readAllowedInteger(
    params,
    'pageSize',
    MEMORY_TOOL_CALL_PAGE_SIZES,
    MEMORY_TOOL_CALL_DEFAULT_PAGE_SIZE,
  );
  const requestedPage = readPositiveInteger(params, 'page', 1);

  return {
    page: (requestedPage - 1) * pageSize <= MAXIMUM_OFFSET ? requestedPage : 1,
    pageSize,
    search: (params.get('search')?.trim() ?? '').slice(
      0,
      MAXIMUM_SEARCH_LENGTH,
    ),
    toolName: (params.get('toolName')?.trim() ?? '').slice(
      0,
      MAXIMUM_SEARCH_LENGTH,
    ),
    sortBy: readAllowedValue(
      params,
      'sortBy',
      MEMORY_TOOL_CALL_SORT_FIELDS,
      'Timestamp',
    ),
    sortDirection: readAllowedValue(
      params,
      'sortDirection',
      SORT_DIRECTIONS,
      'Descending',
    ),
  };
}

export function memoryToolCallSearchQueryParams(
  request: MemoryToolCallSearchRequest,
): ListQueryParams {
  return {
    page: omitDefault(request.page, 1),
    pageSize: omitDefault(
      request.pageSize,
      MEMORY_TOOL_CALL_DEFAULT_PAGE_SIZE,
    ),
    search: omitEmpty(request.search),
    toolName: omitEmpty(request.toolName),
    sortBy: omitDefault(request.sortBy, 'Timestamp'),
    sortDirection: omitDefault(request.sortDirection, 'Descending'),
  } satisfies Params;
}

export function equalMemoryToolCallSearchRequest(
  left: MemoryToolCallSearchRequest,
  right: MemoryToolCallSearchRequest,
): boolean {
  return (
    left.page === right.page &&
    left.pageSize === right.pageSize &&
    left.search === right.search &&
    left.toolName === right.toolName &&
    left.sortBy === right.sortBy &&
    left.sortDirection === right.sortDirection
  );
}
