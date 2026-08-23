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
  SkillSearchRequest,
  SkillSearchSortField,
} from '../data-access/skill.models';

export const SKILL_DEFAULT_PAGE_SIZE = 5;
export const SKILL_PAGE_SIZES = [5, 10, 25] as const;
const MAXIMUM_OFFSET = 100_000;
const MAXIMUM_FILTER_LENGTH = 500;

const SKILL_SORT_FIELDS: readonly SkillSearchSortField[] = [
  'Name',
  'ReferenceCount',
  'AttachmentCount',
];
const SORT_DIRECTIONS: readonly ListSortDirection[] = [
  'Ascending',
  'Descending',
];

export const DEFAULT_SKILL_SEARCH_REQUEST: SkillSearchRequest = {
  page: 1,
  pageSize: SKILL_DEFAULT_PAGE_SIZE,
  search: '',
  tag: '',
  hasReferences: null,
  hasAttachments: null,
  sortBy: 'Name',
  sortDirection: 'Ascending',
};

export function parseSkillSearchRequest(params: ParamMap): SkillSearchRequest {
  const pageSize = readAllowedInteger(
    params,
    'pageSize',
    SKILL_PAGE_SIZES,
    SKILL_DEFAULT_PAGE_SIZE,
  );
  const requestedPage = readPositiveInteger(params, 'page', 1);

  return {
    page:
      (requestedPage - 1) * pageSize <= MAXIMUM_OFFSET ? requestedPage : 1,
    pageSize,
    search: boundedValue(params, 'search'),
    tag: boundedValue(params, 'tag'),
    hasReferences: readNullableBoolean(params, 'hasReferences'),
    hasAttachments: readNullableBoolean(params, 'hasAttachments'),
    sortBy: readAllowedValue(params, 'sortBy', SKILL_SORT_FIELDS, 'Name'),
    sortDirection: readAllowedValue(
      params,
      'sortDirection',
      SORT_DIRECTIONS,
      'Ascending',
    ),
  };
}

export function skillSearchQueryParams(
  request: SkillSearchRequest,
): ListQueryParams {
  return {
    page: omitDefault(request.page, 1),
    pageSize: omitDefault(request.pageSize, SKILL_DEFAULT_PAGE_SIZE),
    search: omitEmpty(request.search),
    tag: omitEmpty(request.tag),
    hasReferences: serializeNullableBoolean(request.hasReferences),
    hasAttachments: serializeNullableBoolean(request.hasAttachments),
    sortBy: omitDefault(request.sortBy, 'Name'),
    sortDirection: omitDefault(request.sortDirection, 'Ascending'),
  } satisfies Params;
}

export function equalSkillSearchRequest(
  left: SkillSearchRequest,
  right: SkillSearchRequest,
): boolean {
  return (
    left.page === right.page &&
    left.pageSize === right.pageSize &&
    left.search === right.search &&
    left.tag === right.tag &&
    left.hasReferences === right.hasReferences &&
    left.hasAttachments === right.hasAttachments &&
    left.sortBy === right.sortBy &&
    left.sortDirection === right.sortDirection
  );
}

function boundedValue(params: ParamMap, key: string): string {
  return (params.get(key)?.trim() ?? '').slice(0, MAXIMUM_FILTER_LENGTH);
}

function readNullableBoolean(params: ParamMap, key: string): boolean | null {
  const value = params.get(key);

  return value === 'true' ? true : value === 'false' ? false : null;
}

function serializeNullableBoolean(value: boolean | null): string | null {
  return value === null ? null : String(value);
}
