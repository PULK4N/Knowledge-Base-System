import { EMPTY_SPECIFICATION } from '../data-access/feature.models';
import { convertToParamMap } from '@angular/router';
import { FeaturePlan, FeatureRecord } from '../data-access/feature.models';
import {
  featureRecordTypes,
  parseFeatureDetailListState,
  selectFeaturePlans,
  selectFeatureRecords,
  selectFeatureReviewers,
} from './feature-detail-list-state';

const plans: readonly FeaturePlan[] = [
  {
    id: 'plan-1',
    title: 'Backend projection',
    content: 'Create the query projection.',
    contentType: 'Markdown',
    additionalInfo: '',
    status: 'NotStarted',
    branches: [],
    previousPlanId: null,
    recordIds: [],
    specification: EMPTY_SPECIFICATION,
    reviewNotes: [],
    reviewers: [],
    createdAt: '2026-08-20T10:00:00Z',
    updatedAt: '2026-08-21T10:00:00Z',
  },
  {
    id: 'plan-2',
    title: 'Angular list',
    content: '<p>Add routed list state.</p>',
    contentType: 'Html',
    additionalInfo: '',
    status: 'NotStarted',
    branches: [],
    previousPlanId: null,
    recordIds: [],
    specification: EMPTY_SPECIFICATION,
    reviewNotes: [],
    reviewers: [],
    createdAt: '2026-08-22T10:00:00Z',
    updatedAt: '2026-08-23T10:00:00Z',
  },
];

const records: readonly FeatureRecord[] = [
  {
    id: 'research-1',
    type: 'research',
    title: 'Routed list state',
    content: 'The route owns list state.',
    sourceType: 'Code',
    sourceReference: 'feature-details.page.ts',
    createdAt: '2026-08-20T10:00:00Z',
    updatedAt: '2026-08-20T10:00:00Z',
  },
  {
    id: 'research-2',
    type: 'research',
    title: 'Shareable browser state',
    content: 'The browser URL is shareable.',
    sourceType: 'Web',
    sourceReference: 'https://example.com/routing',
    createdAt: '2026-08-21T10:00:00Z',
    updatedAt: '2026-08-22T10:00:00Z',
  },
  {
    id: 'record-1',
    type: 'conversation',
    title: 'Keep this original',
    content: '## User\n\nKeep this original.\n\n## AI\n\nDone.',
    sourceType: null,
    sourceReference: '',
    createdAt: '2026-08-20T10:00:00Z',
    updatedAt: '2026-08-23T10:00:00Z',
  },
];

describe('Feature detail client list state', () => {
  it('restores the reviewers tab and filters reviewers by level and description', () => {
    const state = parseFeatureDetailListState(convertToParamMap({ tab: 'reviewers', reviewerSearch: 'permissions', reviewerLevel: 'Heavy' }));
    expect(state.activeTab).toBe('reviewers');
    const common = { title: 'Security', description: 'Check permissions', createdAt: '2026-11-08', updatedAt: '2026-11-08' };
    const shown = selectFeatureReviewers([
      { ...common, id: 'heavy', reviewLevel: 'Heavy' },
      { ...common, id: 'easy', reviewLevel: 'Easy' },
      { ...common, id: 'unrelated', description: 'Check routing', reviewLevel: 'Heavy' },
    ], state.reviewers);
    expect(shown.map(item => item.id)).toEqual(['heavy']);
  });

  it('parses namespaced record state and lowercases the type filter', () => {
    const state = parseFeatureDetailListState(
      convertToParamMap({
        tab: 'records',
        recordSearch: 'route',
        recordType: 'Research',
        recordSort: 'type',
        recordDirection: 'Ascending',
      }),
    );

    expect(state.activeTab).toBe('records');
    expect(state.records).toEqual({
      search: 'route',
      filter: 'research',
      sortBy: 'type',
      sortDirection: 'Ascending',
    });
  });

  it('falls back to the overview for removed tabs', () => {
    expect(
      parseFeatureDetailListState(convertToParamMap({ tab: 'research' }))
        .activeTab,
    ).toBe('overview');
  });

  it('searches, filters, and sorts plans in memory', () => {
    expect(
      selectFeaturePlans(plans, {
        search: 'angular',
        filter: 'Html',
        sortBy: 'title',
        sortDirection: 'Ascending',
      }).map(plan => plan.id),
    ).toEqual(['plan-2']);
  });

  it.each([
    ['feature-details', 'research', 'updatedAt', ['research-1']],
    ['', 'conversation', 'updatedAt', ['record-1']],
    ['', 'All', 'type', ['record-1', 'research-1', 'research-2']],
    ['shareable browser', 'All', 'title', ['research-2']],
  ] as const)(
    'searches %j, filters by type %j, and sorts by %j',
    (search, filter, sortBy, expected) => {
      expect(
        selectFeatureRecords(records, {
          search,
          filter,
          sortBy,
          sortDirection: 'Ascending',
        }).map(record => record.id),
      ).toEqual(expected);
    },
  );

  it('lists each record type once, sorted', () => {
    expect(featureRecordTypes(records)).toEqual(['conversation', 'research']);
  });
});
