import { convertToParamMap } from '@angular/router';
import {
  DEFAULT_FEATURE_SEARCH_REQUEST,
  featureSearchQueryParams,
  parseFeatureSearchRequest,
} from './feature-list-state';

describe('Feature list route state', () => {
  const projectId = '0198d5d1-7800-7c4f-9f33-2a0e590dd213';

  it('parses every server-backed list option from the URL', () => {
    expect(
      parseFeatureSearchRequest(
        convertToParamMap({
          page: '3',
          pageSize: '12',
          search: ' projection ',
          projectId,
          sortBy: 'PlanCount',
          sortDirection: 'Descending',
        }),
      ),
    ).toEqual({
      page: 3,
      pageSize: 12,
      search: 'projection',
      projectId,
      sortBy: 'PlanCount',
      sortDirection: 'Descending',
    });
  });

  it('falls back to safe defaults for unsupported route values', () => {
    expect(
      parseFeatureSearchRequest(
        convertToParamMap({
          page: '-1',
          pageSize: '100',
          projectId: 'not-a-guid',
          sortBy: 'Status',
          sortDirection: 'Sideways',
        }),
      ),
    ).toEqual(DEFAULT_FEATURE_SEARCH_REQUEST);
  });

  it('prevents URLs from exceeding the backend pagination limit', () => {
    expect(
      parseFeatureSearchRequest(
        convertToParamMap({ page: '100000', pageSize: '24' }),
      ).page,
    ).toBe(1);
  });

  it('omits default values when serializing a canonical URL', () => {
    expect(featureSearchQueryParams(DEFAULT_FEATURE_SEARCH_REQUEST)).toEqual({
      page: null,
      pageSize: null,
      search: null,
      projectId: null,
      sortBy: null,
      sortDirection: null,
    });
  });
});
