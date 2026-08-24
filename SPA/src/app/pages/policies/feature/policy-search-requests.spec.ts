import { Subject } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { PolicySearchRequest } from '../data-access/policy.models';
import { policySearchRequests } from './policy-search-requests';

describe('policySearchRequests', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('waits for typing to settle and searches only for the latest term', () => {
    const searches = new Subject<string>();
    const pages = new Subject<number>();
    const requests: PolicySearchRequest[] = [];
    const subscription = policySearchRequests(searches, pages, 300).subscribe(
      request => requests.push(request),
    );

    searches.next('p');
    vi.advanceTimersByTime(200);
    searches.next('policies');
    vi.advanceTimersByTime(299);

    expect(requests).toEqual([{ page: 1, pageSize: 5, search: '' }]);

    vi.advanceTimersByTime(1);
    expect(requests).toEqual([
      { page: 1, pageSize: 5, search: '' },
      { page: 1, pageSize: 5, search: 'policies' },
    ]);

    subscription.unsubscribe();
  });

  it('keeps pagination immediate and resets the page after a search', () => {
    const searches = new Subject<string>();
    const pages = new Subject<number>();
    const requests: PolicySearchRequest[] = [];
    const subscription = policySearchRequests(searches, pages, 300).subscribe(
      request => requests.push(request),
    );

    pages.next(3);
    expect(requests.at(-1)).toEqual({ page: 3, pageSize: 5, search: '' });

    searches.next('  event sourcing  ');
    vi.advanceTimersByTime(300);
    expect(requests.at(-1)).toEqual({
      page: 1,
      pageSize: 5,
      search: 'event sourcing',
    });

    subscription.unsubscribe();
  });
});
