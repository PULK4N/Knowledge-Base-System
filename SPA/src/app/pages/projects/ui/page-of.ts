import { PagedResult } from '../../../core/store/entity-store.service';

/** Cuts one page out of a list that is already fully loaded. */
export function pageOf<T>(
  items: readonly T[],
  page: number,
  pageSize: number,
): PagedResult<T> {
  const totalPages = Math.max(1, Math.ceil(items.length / pageSize));
  const currentPage = Math.min(Math.max(1, page), totalPages);

  return {
    items: items.slice((currentPage - 1) * pageSize, currentPage * pageSize),
    page: currentPage,
    pageSize,
    totalCount: items.length,
    totalPages: items.length === 0 ? 0 : totalPages,
    hasPreviousPage: currentPage > 1,
    hasNextPage: currentPage < totalPages,
  };
}
