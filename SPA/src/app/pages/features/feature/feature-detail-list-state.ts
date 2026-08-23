import { ParamMap } from '@angular/router';
import {
  readAllowedValue,
} from '../../../shared/list-state/list-route-state';
import {
  ClientListState,
  ListSortDirection,
  compareIsoDate,
  compareText,
  selectClientList,
} from '../../../shared/list-state/list-state';
import {
  FeaturePlan,
  FeatureRecord,
  FeatureResearchDiscovery,
  FeatureResearchDiscoverySourceType,
} from '../data-access/feature.models';

export type FeatureTab = 'overview' | 'plans' | 'research' | 'conversations';
export type FeaturePlanSort = 'updatedAt' | 'createdAt' | 'title';
export type FeaturePlanFilter = 'All' | 'Markdown' | 'Html';
export type FeatureResearchSort =
  | 'updatedAt'
  | 'createdAt'
  | 'title'
  | 'sourceType';
export type FeatureResearchFilter =
  | 'All'
  | FeatureResearchDiscoverySourceType;
export type FeatureRecordSort = 'updatedAt' | 'createdAt' | 'userMessage';
export type FeatureRecordFilter = 'All' | 'Edited' | 'Original';

export interface FeatureDetailListState {
  readonly activeTab: FeatureTab;
  readonly plans: ClientListState<FeaturePlanSort, FeaturePlanFilter>;
  readonly research: ClientListState<
    FeatureResearchSort,
    FeatureResearchFilter
  >;
  readonly conversations: ClientListState<
    FeatureRecordSort,
    FeatureRecordFilter
  >;
}

const FEATURE_TABS: readonly FeatureTab[] = [
  'overview',
  'plans',
  'research',
  'conversations',
];
const SORT_DIRECTIONS: readonly ListSortDirection[] = [
  'Ascending',
  'Descending',
];
const PLAN_SORTS: readonly FeaturePlanSort[] = [
  'updatedAt',
  'createdAt',
  'title',
];
const PLAN_FILTERS: readonly FeaturePlanFilter[] = [
  'All',
  'Markdown',
  'Html',
];
const RESEARCH_SORTS: readonly FeatureResearchSort[] = [
  'updatedAt',
  'createdAt',
  'title',
  'sourceType',
];
const RESEARCH_FILTERS: readonly FeatureResearchFilter[] = [
  'All',
  'Other',
  'Code',
  'Web',
  'Mcp',
];
const RECORD_SORTS: readonly FeatureRecordSort[] = [
  'updatedAt',
  'createdAt',
  'userMessage',
];
const RECORD_FILTERS: readonly FeatureRecordFilter[] = [
  'All',
  'Edited',
  'Original',
];

export function parseFeatureDetailListState(
  params: ParamMap,
): FeatureDetailListState {
  return {
    activeTab: readAllowedValue(params, 'tab', FEATURE_TABS, 'overview'),
    plans: {
      search: params.get('planSearch')?.trim() ?? '',
      filter: readAllowedValue(params, 'planType', PLAN_FILTERS, 'All'),
      sortBy: readAllowedValue(params, 'planSort', PLAN_SORTS, 'updatedAt'),
      sortDirection: readAllowedValue(
        params,
        'planDirection',
        SORT_DIRECTIONS,
        'Descending',
      ),
    },
    research: {
      search: params.get('researchSearch')?.trim() ?? '',
      filter: readAllowedValue(
        params,
        'researchSource',
        RESEARCH_FILTERS,
        'All',
      ),
      sortBy: readAllowedValue(
        params,
        'researchSort',
        RESEARCH_SORTS,
        'updatedAt',
      ),
      sortDirection: readAllowedValue(
        params,
        'researchDirection',
        SORT_DIRECTIONS,
        'Descending',
      ),
    },
    conversations: {
      search: params.get('conversationSearch')?.trim() ?? '',
      filter: readAllowedValue(
        params,
        'conversationFilter',
        RECORD_FILTERS,
        'All',
      ),
      sortBy: readAllowedValue(
        params,
        'conversationSort',
        RECORD_SORTS,
        'updatedAt',
      ),
      sortDirection: readAllowedValue(
        params,
        'conversationDirection',
        SORT_DIRECTIONS,
        'Descending',
      ),
    },
  };
}

export function selectFeaturePlans(
  plans: readonly FeaturePlan[],
  state: FeatureDetailListState['plans'],
): readonly FeaturePlan[] {
  return selectClientList(plans, state, {
    searchText: plan => `${plan.title}\n${plan.content}`,
    matchesFilter: (plan, filter) =>
      filter === 'All' || plan.contentType === filter,
    compare: (left, right, sortBy) => {
      switch (sortBy) {
        case 'title':
          return compareText(left.title, right.title);
        case 'createdAt':
          return compareIsoDate(left.createdAt, right.createdAt);
        case 'updatedAt':
          return compareIsoDate(left.updatedAt, right.updatedAt);
      }
    },
  });
}

export function selectFeatureResearch(
  discoveries: readonly FeatureResearchDiscovery[],
  state: FeatureDetailListState['research'],
): readonly FeatureResearchDiscovery[] {
  return selectClientList(discoveries, state, {
    searchText: discovery =>
      `${discovery.title}\n${discovery.content}\n${discovery.sourceReference}\n${discovery.sourceType}`,
    matchesFilter: (discovery, filter) =>
      filter === 'All' || discovery.sourceType === filter,
    compare: (left, right, sortBy) => {
      switch (sortBy) {
        case 'title':
          return compareText(left.title, right.title);
        case 'sourceType':
          return compareText(left.sourceType, right.sourceType);
        case 'createdAt':
          return compareIsoDate(left.createdAt, right.createdAt);
        case 'updatedAt':
          return compareIsoDate(left.updatedAt, right.updatedAt);
      }
    },
  });
}

export function selectFeatureRecords(
  records: readonly FeatureRecord[],
  state: FeatureDetailListState['conversations'],
): readonly FeatureRecord[] {
  return selectClientList(records, state, {
    searchText: record => `${record.userMessage}\n${record.aiAnswer}`,
    matchesFilter: (record, filter) => {
      if (filter === 'All') return true;

      const edited = record.createdAt !== record.updatedAt;
      return filter === 'Edited' ? edited : !edited;
    },
    compare: (left, right, sortBy) => {
      switch (sortBy) {
        case 'userMessage':
          return compareText(left.userMessage, right.userMessage);
        case 'createdAt':
          return compareIsoDate(left.createdAt, right.createdAt);
        case 'updatedAt':
          return compareIsoDate(left.updatedAt, right.updatedAt);
      }
    },
  });
}
