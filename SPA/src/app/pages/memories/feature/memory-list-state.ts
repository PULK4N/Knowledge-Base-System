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
const MAXIMUM_MODEL_LENGTH = 200;

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
  model: '',
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
  const search = boundedValue(params, 'search');
  const isSearching = Boolean(search || semanticSearch);
  const defaultSort: MemorySearchSortField = isSearching
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
    model: boundedValue(params, 'model', MAXIMUM_MODEL_LENGTH),
    hasSummary: readNullableBoolean(params, 'hasSummary'),
    minimumPromptCount: readNullableNonNegativeInteger(
      params,
      'minimumPromptCount',
    ),
    sortBy:
      !isSearching && parsedSort === 'Relevance'
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
  const defaultSort = isMemorySearch(request) ? 'Relevance' : 'LastActivity';

  return {
    page: omitDefault(request.page, 1),
    pageSize: omitDefault(request.pageSize, MEMORY_DEFAULT_PAGE_SIZE),
    search: omitEmpty(request.search),
    semanticSearch: omitEmpty(request.semanticSearch),
    model: omitEmpty(request.model),
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
    left.model === right.model &&
    left.hasSummary === right.hasSummary &&
    left.minimumPromptCount === right.minimumPromptCount &&
    left.sortBy === right.sortBy &&
    left.sortDirection === right.sortDirection
  );
}

/** True when the text search or the vector search has input. */
export function isMemorySearch(request: MemorySearchRequest): boolean {
  return (
    request.search.trim().length > 0 ||
    request.semanticSearch.trim().length > 0
  );
}

function boundedValue(
  params: ParamMap,
  key: string,
  maximumLength = MAXIMUM_SEARCH_LENGTH,
): string {
  return (params.get(key)?.trim() ?? '').slice(0, maximumLength);
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
