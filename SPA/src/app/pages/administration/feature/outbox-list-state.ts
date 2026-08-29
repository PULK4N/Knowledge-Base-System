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
  OutboxPayloadSearchRequest,
  OutboxPayloadSortField,
} from '../data-access/outbox-administration.models';

export const OUTBOX_DEFAULT_PAGE_SIZE = 10;
export const OUTBOX_PAGE_SIZES = [10, 25, 50] as const;
export const OUTBOX_STATES = [
  'New',
  'Reading',
  'Error',
  'Sent',
] as const;

const MAXIMUM_OFFSET = 100_000;
const MAXIMUM_FILTER_LENGTH = 500;
const OUTBOX_SORT_FIELDS: readonly OutboxPayloadSortField[] = [
  'Id',
  'State',
  'RetryCount',
  'AggregateId',
];
const SORT_DIRECTIONS: readonly ListSortDirection[] = [
  'Ascending',
  'Descending',
];

export const DEFAULT_OUTBOX_SEARCH_REQUEST: OutboxPayloadSearchRequest = {
  page: 1,
  pageSize: OUTBOX_DEFAULT_PAGE_SIZE,
  search: '',
  onlyIncomplete: false,
  state: '',
  aggregateId: '',
  sortBy: 'Id',
  sortDirection: 'Descending',
};

export function parseOutboxSearchRequest(
  params: ParamMap,
): OutboxPayloadSearchRequest {
  const pageSize = readAllowedInteger(
    params,
    'pageSize',
    OUTBOX_PAGE_SIZES,
    OUTBOX_DEFAULT_PAGE_SIZE,
  );
  const requestedPage = readPositiveInteger(params, 'page', 1);

  return {
    page: (requestedPage - 1) * pageSize <= MAXIMUM_OFFSET ? requestedPage : 1,
    pageSize,
    search: boundedValue(params, 'search'),
    onlyIncomplete: params.get('onlyIncomplete') === 'true',
    state: readAllowedValue(params, 'state', OUTBOX_STATES, ''),
    aggregateId: boundedValue(params, 'aggregateId'),
    sortBy: readAllowedValue(params, 'sortBy', OUTBOX_SORT_FIELDS, 'Id'),
    sortDirection: readAllowedValue(
      params,
      'sortDirection',
      SORT_DIRECTIONS,
      'Descending',
    ),
  };
}

export function outboxSearchQueryParams(
  request: OutboxPayloadSearchRequest,
): ListQueryParams {
  return {
    page: omitDefault(request.page, 1),
    pageSize: omitDefault(request.pageSize, OUTBOX_DEFAULT_PAGE_SIZE),
    search: omitEmpty(request.search),
    onlyIncomplete: request.onlyIncomplete ? 'true' : null,
    state: omitEmpty(request.state),
    aggregateId: omitEmpty(request.aggregateId),
    sortBy: omitDefault(request.sortBy, 'Id'),
    sortDirection: omitDefault(request.sortDirection, 'Descending'),
  } satisfies Params;
}

export function equalOutboxSearchRequest(
  left: OutboxPayloadSearchRequest,
  right: OutboxPayloadSearchRequest,
): boolean {
  return (
    left.page === right.page &&
    left.pageSize === right.pageSize &&
    left.search === right.search &&
    left.onlyIncomplete === right.onlyIncomplete &&
    left.state === right.state &&
    left.aggregateId === right.aggregateId &&
    left.sortBy === right.sortBy &&
    left.sortDirection === right.sortDirection
  );
}

function boundedValue(params: ParamMap, key: string): string {
  return (params.get(key)?.trim() ?? '').slice(0, MAXIMUM_FILTER_LENGTH);
}
