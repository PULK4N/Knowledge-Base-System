import { convertToParamMap } from '@angular/router';
import {
  DEFAULT_SKILL_SEARCH_REQUEST,
  parseSkillSearchRequest,
  skillSearchQueryParams,
} from './skill-list-state';

describe('Skill list route state', () => {
  it('parses every server-backed list option from the URL', () => {
    expect(
      parseSkillSearchRequest(
        convertToParamMap({
          page: '3',
          pageSize: '10',
          search: ' projection ',
          tag: ' event-sourcing ',
          hasReferences: 'true',
          hasAttachments: 'false',
          sortBy: 'ReferenceCount',
          sortDirection: 'Descending',
        }),
      ),
    ).toEqual({
      page: 3,
      pageSize: 10,
      search: 'projection',
      tag: 'event-sourcing',
      hasReferences: true,
      hasAttachments: false,
      sortBy: 'ReferenceCount',
      sortDirection: 'Descending',
    });
  });

  it('falls back to safe defaults for unsupported route values', () => {
    expect(
      parseSkillSearchRequest(
        convertToParamMap({
          page: '-1',
          pageSize: '100',
          hasReferences: 'sometimes',
          hasAttachments: '1',
          sortBy: 'UpdatedAt',
          sortDirection: 'Sideways',
        }),
      ),
    ).toEqual(DEFAULT_SKILL_SEARCH_REQUEST);
  });

  it('prevents URLs from exceeding the backend pagination limit', () => {
    expect(
      parseSkillSearchRequest(
        convertToParamMap({ page: '100000', pageSize: '25' }),
      ).page,
    ).toBe(1);
  });

  it('serializes all non-default values into a canonical URL', () => {
    expect(
      skillSearchQueryParams({
        page: 2,
        pageSize: 10,
        search: 'projection',
        tag: 'event-sourcing',
        hasReferences: true,
        hasAttachments: false,
        sortBy: 'AttachmentCount',
        sortDirection: 'Descending',
      }),
    ).toEqual({
      page: 2,
      pageSize: 10,
      search: 'projection',
      tag: 'event-sourcing',
      hasReferences: 'true',
      hasAttachments: 'false',
      sortBy: 'AttachmentCount',
      sortDirection: 'Descending',
    });
  });

  it('omits default values when serializing a canonical URL', () => {
    expect(skillSearchQueryParams(DEFAULT_SKILL_SEARCH_REQUEST)).toEqual({
      page: null,
      pageSize: null,
      search: null,
      tag: null,
      hasReferences: null,
      hasAttachments: null,
      sortBy: null,
      sortDirection: null,
    });
  });
});
