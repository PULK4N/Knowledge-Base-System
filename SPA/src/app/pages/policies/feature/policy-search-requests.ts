import {
  Observable,
  debounceTime,
  distinctUntilChanged,
  map,
  merge,
  scan,
  startWith,
} from 'rxjs';
import { PolicySearchRequest } from '../data-access/policy.models';

export const POLICY_SEARCH_DEBOUNCE_MS = 400;

type PolicySearchRequestChange =
  | { readonly page: number }
  | { readonly page: 1; readonly search: string };

export function policySearchRequests(
  searchRequests: Observable<string>,
  pageRequests: Observable<number>,
  debounceMilliseconds = POLICY_SEARCH_DEBOUNCE_MS,
): Observable<PolicySearchRequest> {
  const initialRequest: PolicySearchRequest = {
    page: 1,
    pageSize: 5,
    search: '',
  };

  return merge(
    pageRequests.pipe(map(page => ({ page }) as const)),
    searchRequests.pipe(
      map(search => search.trim()),
      debounceTime(debounceMilliseconds),
      distinctUntilChanged(),
      map(search => ({ page: 1, search }) as const),
    ),
  ).pipe(
    scan<PolicySearchRequestChange, PolicySearchRequest>(
      (request, change) => ({ ...request, ...change }),
      initialRequest,
    ),
    startWith(initialRequest),
    distinctUntilChanged(
      (previous, current) =>
        previous.page === current.page &&
        previous.pageSize === current.pageSize &&
        previous.search === current.search,
    ),
  );
}
