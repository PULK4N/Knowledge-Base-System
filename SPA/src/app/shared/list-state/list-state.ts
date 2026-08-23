export type ListSortDirection = 'Ascending' | 'Descending';

export interface ClientListState<TSort extends string, TFilter extends string> {
  readonly search: string;
  readonly filter: TFilter;
  readonly sortBy: TSort;
  readonly sortDirection: ListSortDirection;
}

export interface ClientListDefinition<
  TItem,
  TSort extends string,
  TFilter extends string,
> {
  readonly searchText: (item: TItem) => string;
  readonly matchesFilter: (item: TItem, filter: TFilter) => boolean;
  readonly compare: (left: TItem, right: TItem, sortBy: TSort) => number;
}

export function selectClientList<
  TItem,
  TSort extends string,
  TFilter extends string,
>(
  items: readonly TItem[],
  state: ClientListState<TSort, TFilter>,
  definition: ClientListDefinition<TItem, TSort, TFilter>,
): readonly TItem[] {
  const search = normalizeSearch(state.search);
  const direction = state.sortDirection === 'Descending' ? -1 : 1;

  return items
    .map((item, index) => ({ item, index }))
    .filter(
      entry =>
        definition.matchesFilter(entry.item, state.filter) &&
        (!search ||
          normalizeSearch(definition.searchText(entry.item)).includes(search)),
    )
    .sort((left, right) => {
      const compared =
        definition.compare(left.item, right.item, state.sortBy) * direction;

      return compared || left.index - right.index;
    })
    .map(entry => entry.item);
}

export function normalizeSearch(value: string): string {
  return value.trim().toLocaleLowerCase();
}

export function compareText(left: string, right: string): number {
  return left.localeCompare(right, undefined, { sensitivity: 'base' });
}

export function compareIsoDate(left: string, right: string): number {
  return Date.parse(left) - Date.parse(right);
}
