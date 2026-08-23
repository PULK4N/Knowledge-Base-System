import { convertToParamMap } from '@angular/router';
import {
  FeaturePlan,
  FeatureRecord,
  FeatureResearchDiscovery,
} from '../data-access/feature.models';
import {
  parseFeatureDetailListState,
  selectFeaturePlans,
  selectFeatureRecords,
  selectFeatureResearch,
} from './feature-detail-list-state';

const plans: readonly FeaturePlan[] = [
  {
    id: 'plan-1',
    title: 'Backend projection',
    content: 'Create the query projection.',
    contentType: 'Markdown',
    createdAt: '2026-08-20T10:00:00Z',
    updatedAt: '2026-08-21T10:00:00Z',
  },
  {
    id: 'plan-2',
    title: 'Angular list',
    content: '<p>Add routed list state.</p>',
    contentType: 'Html',
    createdAt: '2026-08-22T10:00:00Z',
    updatedAt: '2026-08-23T10:00:00Z',
  },
];

const research: readonly FeatureResearchDiscovery[] = [
  {
    id: 'research-1',
    title: 'Routed list state',
    content: 'The route owns list state.',
    sourceType: 'Code',
    sourceReference: 'feature-details.page.ts',
    createdAt: '2026-08-20T10:00:00Z',
    updatedAt: '2026-08-20T10:00:00Z',
  },
  {
    id: 'research-2',
    title: 'Shareable browser state',
    content: 'The browser URL is shareable.',
    sourceType: 'Web',
    sourceReference: 'https://example.com/routing',
    createdAt: '2026-08-21T10:00:00Z',
    updatedAt: '2026-08-22T10:00:00Z',
  },
];

const records: readonly FeatureRecord[] = [
  {
    id: 'record-1',
    userMessage: 'Keep this original.',
    aiAnswer: 'Done.',
    createdAt: '2026-08-20T10:00:00Z',
    updatedAt: '2026-08-20T10:00:00Z',
  },
  {
    id: 'record-2',
    userMessage: 'Revise the frontend.',
    aiAnswer: 'Updated.',
    createdAt: '2026-08-21T10:00:00Z',
    updatedAt: '2026-08-22T10:00:00Z',
  },
];

describe('Feature detail client list state', () => {
  it('parses namespaced tab state from query parameters', () => {
    const state = parseFeatureDetailListState(
      convertToParamMap({
        tab: 'research',
        researchSearch: 'route',
        researchSource: 'Code',
        researchSort: 'sourceType',
        researchDirection: 'Ascending',
      }),
    );

    expect(state.activeTab).toBe('research');
    expect(state.research).toEqual({
      search: 'route',
      filter: 'Code',
      sortBy: 'sourceType',
      sortDirection: 'Ascending',
    });
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

  it('searches provenance and filters research by source', () => {
    expect(
      selectFeatureResearch(research, {
        search: 'feature-details',
        filter: 'Code',
        sortBy: 'updatedAt',
        sortDirection: 'Descending',
      }).map(discovery => discovery.id),
    ).toEqual(['research-1']);
  });

  it('searches and sorts research by title', () => {
    expect(
      selectFeatureResearch(research, {
        search: 'shareable browser',
        filter: 'All',
        sortBy: 'title',
        sortDirection: 'Ascending',
      }).map(discovery => discovery.id),
    ).toEqual(['research-2']);
  });

  it('filters edited conversations and sorts them without mutating input', () => {
    const originalOrder = records.map(record => record.id);
    const result = selectFeatureRecords(records, {
      search: '',
      filter: 'Edited',
      sortBy: 'updatedAt',
      sortDirection: 'Descending',
    });

    expect(result.map(record => record.id)).toEqual(['record-2']);
    expect(records.map(record => record.id)).toEqual(originalOrder);
  });
});
