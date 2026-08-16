import { visiblePages } from './pagination';

describe('visiblePages', () => {
  it('shows every page when the result fits in the visible range', () => {
    expect(visiblePages(3, 1)).toEqual([1, 2, 3]);
  });

  it('keeps the current page centered away from the boundaries', () => {
    expect(visiblePages(12, 7)).toEqual([5, 6, 7, 8, 9]);
  });

  it('keeps the final range within the available pages', () => {
    expect(visiblePages(12, 12)).toEqual([8, 9, 10, 11, 12]);
  });
});
