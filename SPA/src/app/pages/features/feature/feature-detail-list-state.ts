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
  FeatureReviewNote,
} from '../data-access/feature.models';
import { flattenBranches } from '../ui/feature-plan-branches';

export type FeatureTab =
  | 'overview'
  | 'plans'
  | 'records'
  | 'review-notes'
  | 'history';
export type FeaturePlanSort = 'updatedAt' | 'createdAt' | 'title';
export type FeaturePlanFilter = 'All' | 'Markdown' | 'Html';
export type FeatureRecordSort = 'updatedAt' | 'createdAt' | 'title' | 'type';
/** 'All' or a record type; types are free text, so any value is allowed. */
export type FeatureRecordFilter = string;
export type FeatureReviewNoteSort = 'updatedAt' | 'createdAt' | 'title';
export type FeatureReviewNoteFilter = 'All' | 'Edited' | 'Original';

export interface FeatureDetailListState {
  readonly activeTab: FeatureTab;
  readonly plans: ClientListState<FeaturePlanSort, FeaturePlanFilter>;
  readonly records: ClientListState<FeatureRecordSort, FeatureRecordFilter>;
  readonly reviewNotes: ClientListState<
    FeatureReviewNoteSort,
    FeatureReviewNoteFilter
  >;
}

const FEATURE_TABS: readonly FeatureTab[] = [
  'overview',
  'plans',
  'records',
  'review-notes',
  'history',
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
const RECORD_SORTS: readonly FeatureRecordSort[] = [
  'updatedAt',
  'createdAt',
  'title',
  'type',
];
const REVIEW_NOTE_SORTS: readonly FeatureReviewNoteSort[] = [
  'updatedAt',
  'createdAt',
  'title',
];
const REVIEW_NOTE_FILTERS: readonly FeatureReviewNoteFilter[] = [
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
    records: {
      search: params.get('recordSearch')?.trim() ?? '',
      filter: params.get('recordType')?.trim().toLowerCase() || 'All',
      sortBy: readAllowedValue(params, 'recordSort', RECORD_SORTS, 'updatedAt'),
      sortDirection: readAllowedValue(
        params,
        'recordDirection',
        SORT_DIRECTIONS,
        'Descending',
      ),
    },
    reviewNotes: {
      search: params.get('reviewNoteSearch')?.trim() ?? '',
      filter: readAllowedValue(
        params,
        'reviewNoteFilter',
        REVIEW_NOTE_FILTERS,
        'All',
      ),
      sortBy: readAllowedValue(
        params,
        'reviewNoteSort',
        REVIEW_NOTE_SORTS,
        'updatedAt',
      ),
      sortDirection: readAllowedValue(
        params,
        'reviewNoteDirection',
        SORT_DIRECTIONS,
        'Descending',
      ),
    },
  };
}

export function selectFeatureReviewNotes(
  reviewNotes: readonly FeatureReviewNote[],
  state: FeatureDetailListState['reviewNotes'],
): readonly FeatureReviewNote[] {
  return selectClientList(reviewNotes, state, {
    searchText: reviewNote => `${reviewNote.title}\n${reviewNote.content}`,
    matchesFilter: (reviewNote, filter) => {
      if (filter === 'All') return true;

      const edited = reviewNote.createdAt !== reviewNote.updatedAt;
      return filter === 'Edited' ? edited : !edited;
    },
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

export function selectFeaturePlans(
  plans: readonly FeaturePlan[],
  state: FeatureDetailListState['plans'],
): readonly FeaturePlan[] {
  return selectClientList(plans, state, {
    searchText: plan =>
      [
        plan.title,
        plan.additionalInfo,
        ...flattenBranches(plan.branch).map(branch => branch.name),
        plan.content,
      ].join('\n'),
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

export function selectFeatureRecords(
  records: readonly FeatureRecord[],
  state: FeatureDetailListState['records'],
): readonly FeatureRecord[] {
  return selectClientList(records, state, {
    searchText: record =>
      [
        record.title,
        record.type,
        record.content,
        record.sourceReference,
        record.sourceType ?? '',
      ].join('\n'),
    matchesFilter: (record, filter) =>
      filter === 'All' || record.type === filter,
    compare: (left, right, sortBy) => {
      switch (sortBy) {
        case 'title':
          return compareText(left.title, right.title);
        case 'type':
          return compareText(left.type, right.type);
        case 'createdAt':
          return compareIsoDate(left.createdAt, right.createdAt);
        case 'updatedAt':
          return compareIsoDate(left.updatedAt, right.updatedAt);
      }
    },
  });
}

/** Distinct record types, sorted, for the type filter. */
export function featureRecordTypes(
  records: readonly FeatureRecord[],
): readonly string[] {
  return [...new Set(records.map(record => record.type))].sort(compareText);
}
