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
  MemorySearchRequest,
  MemorySearchSortField,
} from '../data-access/memory.models';

export const MEMORY_DEFAULT_PAGE_SIZE = 5;
export const MEMORY_PAGE_SIZES = [5, 10, 25] as const;
const MAXIMUM_OFFSET = 100_000;
const MAXIMUM_SEARCH_LENGTH = 500;

const MEMORY_SORT_FIELDS: readonly MemorySearchSortField[] = [
  'Relevance',
  'LastActivity',
  'PromptCount',
  'FirstPrompt',
  'LastPrompt',
  'SummaryUpdated',
];
const SORT_DIRECTIONS: readonly ListSortDirection[] = [
  'Ascending',
  'Descending',
];

export const DEFAULT_MEMORY_SEARCH_REQUEST: MemorySearchRequest = {
  page: 1,
  pageSize: MEMORY_DEFAULT_PAGE_SIZE,
  search: '',
  semanticSearch: '',
  hasSummary: null,
  minimumPromptCount: null,
  sortBy: 'LastActivity',
  sortDirection: 'Descending',
};

export function parseMemorySearchRequest(
  params: ParamMap,
): MemorySearchRequest {
  const pageSize = readAllowedInteger(
    params,
    'pageSize',
    MEMORY_PAGE_SIZES,
    MEMORY_DEFAULT_PAGE_SIZE,
  );
  const requestedPage = readPositiveInteger(params, 'page', 1);
  const semanticSearch = boundedValue(params, 'semanticSearch');
  const search = semanticSearch ? '' : boundedValue(params, 'search');
  const defaultSort: MemorySearchSortField = semanticSearch
    ? 'Relevance'
    : 'LastActivity';
  const parsedSort = readAllowedValue(
    params,
    'sortBy',
    MEMORY_SORT_FIELDS,
    defaultSort,
  );

  return {
    page:
      (requestedPage - 1) * pageSize <= MAXIMUM_OFFSET ? requestedPage : 1,
    pageSize,
    search,
    semanticSearch,
    hasSummary: readNullableBoolean(params, 'hasSummary'),
    minimumPromptCount: readNullableNonNegativeInteger(
      params,
      'minimumPromptCount',
    ),
    sortBy:
      !semanticSearch && parsedSort === 'Relevance'
        ? 'LastActivity'
        : parsedSort,
    sortDirection: readAllowedValue(
      params,
      'sortDirection',
      SORT_DIRECTIONS,
      'Descending',
    ),
  };
}

export function memorySearchQueryParams(
  request: MemorySearchRequest,
): ListQueryParams {
  const semanticSearch = omitEmpty(request.semanticSearch);
  const defaultSort = semanticSearch ? 'Relevance' : 'LastActivity';

  return {
    page: omitDefault(request.page, 1),
    pageSize: omitDefault(request.pageSize, MEMORY_DEFAULT_PAGE_SIZE),
    search: semanticSearch ? null : omitEmpty(request.search),
    semanticSearch,
    hasSummary: serializeNullableBoolean(request.hasSummary),
    minimumPromptCount: request.minimumPromptCount,
    sortBy: omitDefault(request.sortBy, defaultSort),
    sortDirection: omitDefault(request.sortDirection, 'Descending'),
  } satisfies Params;
}

export function equalMemorySearchRequest(
  left: MemorySearchRequest,
  right: MemorySearchRequest,
): boolean {
  return (
    left.page === right.page &&
    left.pageSize === right.pageSize &&
    left.search === right.search &&
    left.semanticSearch === right.semanticSearch &&
    left.hasSummary === right.hasSummary &&
    left.minimumPromptCount === right.minimumPromptCount &&
    left.sortBy === right.sortBy &&
    left.sortDirection === right.sortDirection
  );
}

function boundedValue(params: ParamMap, key: string): string {
  return (params.get(key)?.trim() ?? '').slice(0, MAXIMUM_SEARCH_LENGTH);
}

function readNullableBoolean(params: ParamMap, key: string): boolean | null {
  const value = params.get(key);

  return value === 'true' ? true : value === 'false' ? false : null;
}

function readNullableNonNegativeInteger(
  params: ParamMap,
  key: string,
): number | null {
  const rawValue = params.get(key);
  if (rawValue === null || rawValue.trim() === '') return null;

  const value = Number(rawValue);
  return Number.isSafeInteger(value) && value >= 1 ? value : null;
}

function serializeNullableBoolean(value: boolean | null): string | null {
  return value === null ? null : String(value);
}
