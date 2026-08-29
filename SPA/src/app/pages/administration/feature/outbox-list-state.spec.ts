import { convertToParamMap } from '@angular/router';
import {
  DEFAULT_OUTBOX_SEARCH_REQUEST,
  outboxSearchQueryParams,
  parseOutboxSearchRequest,
} from './outbox-list-state';

describe('Outbox list route state', () => {
  it('parses every server-backed list option from the URL', () => {
    expect(
      parseOutboxSearchRequest(
        convertToParamMap({
          page: '3',
          pageSize: '25',
          search: ' SkillUpdated ',
          onlyIncomplete: 'true',
          state: 'Error',
          aggregateId: ' aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa ',
          sortBy: 'RetryCount',
          sortDirection: 'Ascending',
        }),
      ),
    ).toEqual({
      page: 3,
      pageSize: 25,
      search: 'SkillUpdated',
      onlyIncomplete: true,
      state: 'Error',
      aggregateId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      sortBy: 'RetryCount',
      sortDirection: 'Ascending',
    });
  });

  it('falls back to safe defaults for unsupported route values', () => {
    expect(
      parseOutboxSearchRequest(
        convertToParamMap({
          page: '-1',
          pageSize: '100',
          onlyIncomplete: 'yes',
          state: 'Requeued',
          sortBy: 'Timestamp',
          sortDirection: 'Sideways',
        }),
      ),
    ).toEqual(DEFAULT_OUTBOX_SEARCH_REQUEST);
  });

  it('prevents URLs from exceeding the backend pagination limit', () => {
    expect(
      parseOutboxSearchRequest(
        convertToParamMap({ page: '100000', pageSize: '25' }),
      ).page,
    ).toBe(1);
  });

  it('serializes all non-default values into a canonical URL', () => {
    expect(
      outboxSearchQueryParams({
        page: 2,
        pageSize: 25,
        search: 'SkillUpdated',
        onlyIncomplete: true,
        state: 'Sent',
        aggregateId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        sortBy: 'State',
        sortDirection: 'Ascending',
      }),
    ).toEqual({
      page: 2,
      pageSize: 25,
      search: 'SkillUpdated',
      onlyIncomplete: 'true',
      state: 'Sent',
      aggregateId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      sortBy: 'State',
      sortDirection: 'Ascending',
    });
  });

  it('omits default values when serializing a canonical URL', () => {
    expect(outboxSearchQueryParams(DEFAULT_OUTBOX_SEARCH_REQUEST)).toEqual({
      page: null,
      pageSize: null,
      search: null,
      onlyIncomplete: null,
      state: null,
      aggregateId: null,
      sortBy: null,
      sortDirection: null,
    });
  });
});
