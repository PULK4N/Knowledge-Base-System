import { pageOf } from './page-of';

describe('pageOf', () => {
  const items = Array.from({ length: 23 }, (_, index) => index + 1);

  it('returns the requested page and its neighbours', () => {
    const page = pageOf(items, 2, 10);

    expect(page.items).toEqual([11, 12, 13, 14, 15, 16, 17, 18, 19, 20]);
    expect(page).toMatchObject({
      page: 2,
      totalCount: 23,
      totalPages: 3,
      hasPreviousPage: true,
      hasNextPage: true,
    });
  });

  it('clamps a page past the end, for example after removing the last item of it', () => {
    expect(pageOf(items, 9, 10)).toMatchObject({ page: 3, items: [21, 22, 23] });
    expect(pageOf([], 1, 10)).toMatchObject({ page: 1, totalPages: 0, items: [] });
  });
});
