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
  FeatureSearchRequest,
  FeatureSearchSortField,
} from '../data-access/feature.models';

export const FEATURE_DEFAULT_PAGE_SIZE = 6;
export const FEATURE_PAGE_SIZES = [6, 12, 24] as const;
const MAXIMUM_OFFSET = 100_000;
const MAXIMUM_SEARCH_LENGTH = 500;
const GUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

const FEATURE_SORT_FIELDS: readonly FeatureSearchSortField[] = [
  'Name',
  'PlanCount',
  'RecordCount',
];
const SORT_DIRECTIONS: readonly ListSortDirection[] = [
  'Ascending',
  'Descending',
];

export const DEFAULT_FEATURE_SEARCH_REQUEST: FeatureSearchRequest = {
  page: 1,
  pageSize: FEATURE_DEFAULT_PAGE_SIZE,
  search: '',
  projectId: '',
  sortBy: 'Name',
  sortDirection: 'Ascending',
};

export function parseFeatureSearchRequest(params: ParamMap): FeatureSearchRequest {
  const pageSize = readAllowedInteger(
    params,
    'pageSize',
    FEATURE_PAGE_SIZES,
    FEATURE_DEFAULT_PAGE_SIZE,
  );
  const requestedPage = readPositiveInteger(params, 'page', 1);
  const projectId = params.get('projectId')?.trim() ?? '';

  return {
    page:
      (requestedPage - 1) * pageSize <= MAXIMUM_OFFSET ? requestedPage : 1,
    pageSize,
    search: (params.get('search')?.trim() ?? '').slice(
      0,
      MAXIMUM_SEARCH_LENGTH,
    ),
    projectId: GUID_PATTERN.test(projectId) ? projectId : '',
    sortBy: readAllowedValue(params, 'sortBy', FEATURE_SORT_FIELDS, 'Name'),
    sortDirection: readAllowedValue(
      params,
      'sortDirection',
      SORT_DIRECTIONS,
      'Ascending',
    ),
  };
}

export function featureSearchQueryParams(
  request: FeatureSearchRequest,
): ListQueryParams {
  return {
    page: omitDefault(request.page, 1),
    pageSize: omitDefault(request.pageSize, FEATURE_DEFAULT_PAGE_SIZE),
    search: omitEmpty(request.search),
    projectId: omitEmpty(request.projectId),
    sortBy: omitDefault(request.sortBy, 'Name'),
    sortDirection: omitDefault(request.sortDirection, 'Ascending'),
  } satisfies Params;
}

export function equalFeatureSearchRequest(
  left: FeatureSearchRequest,
  right: FeatureSearchRequest,
): boolean {
  return (
    left.page === right.page &&
    left.pageSize === right.pageSize &&
    left.search === right.search &&
    left.projectId === right.projectId &&
    left.sortBy === right.sortBy &&
    left.sortDirection === right.sortDirection
  );
}
