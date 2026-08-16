export function visiblePages(
  totalPages: number,
  currentPage: number,
  maximumVisible = 5,
): readonly number[] {
  if (totalPages <= 0) return [];

  const count = Math.min(totalPages, maximumVisible);
  const first = Math.min(
    Math.max(1, currentPage - Math.floor(count / 2)),
    totalPages - count + 1,
  );

  return Array.from({ length: count }, (_, index) => first + index);
}
