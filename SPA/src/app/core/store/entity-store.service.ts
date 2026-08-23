import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, distinctUntilChanged, map } from 'rxjs';

export interface BaseEntity {
  readonly id: string;
}

export interface PagedResult<T> {
  readonly items: readonly T[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
  readonly totalPages: number;
  readonly hasPreviousPage: boolean;
  readonly hasNextPage: boolean;
}

interface StoredSearch {
  readonly entityType: string;
  readonly itemIds: readonly string[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
  readonly totalPages: number;
  readonly hasPreviousPage: boolean;
  readonly hasNextPage: boolean;
}

interface EntityStoreState {
  readonly entities: Readonly<
    Record<string, Readonly<Record<string, BaseEntity>>>
  >;
  readonly searches: Readonly<Record<string, StoredSearch>>;
}

const initialState: EntityStoreState = {
  entities: {},
  searches: {},
};

function normalize(value: string): string {
  return value.trim().toLowerCase();
}

@Injectable({ providedIn: 'root' })
export class EntityStore {
  private readonly stateSubject =
    new BehaviorSubject<EntityStoreState>(initialState);

  readonly state$ = this.stateSubject.asObservable();

  entity$<T extends BaseEntity>(
    entityType: string,
    id: string,
  ): Observable<T | undefined> {
    const typeKey = normalize(entityType);
    const idKey = normalize(id);

    return this.state$.pipe(
      map(state => state.entities[typeKey]?.[idKey] as T | undefined),
      distinctUntilChanged(),
    );
  }

  search$<T extends BaseEntity>(
    queryKey: string,
  ): Observable<PagedResult<T> | undefined> {
    return this.state$.pipe(
      map(state => {
        const search = state.searches[queryKey];
        if (!search) return undefined;

        const entities = state.entities[search.entityType] ?? {};
        return {
          items: search.itemIds
            .map(id => entities[id] as T | undefined)
            .filter((entity): entity is T => entity !== undefined),
          page: search.page,
          pageSize: search.pageSize,
          totalCount: search.totalCount,
          totalPages: search.totalPages,
          hasPreviousPage: search.hasPreviousPage,
          hasNextPage: search.hasNextPage,
        };
      }),
      distinctUntilChanged(equalPagedResult),
    );
  }

  upsert<T extends BaseEntity>(entityType: string, entity: T): void {
    const state = this.stateSubject.value;
    const typeKey = normalize(entityType);
    const idKey = normalize(entity.id);
    const entitiesForType = {
      ...(state.entities[typeKey] ?? {}),
      [idKey]: entity,
    };

    this.stateSubject.next({
      entities: {
        ...state.entities,
        [typeKey]: entitiesForType,
      },
      searches: state.searches,
    });
  }

  replaceSearch<T extends BaseEntity>(
    queryKey: string,
    entityType: string,
    result: PagedResult<T>,
  ): void {
    const state = this.stateSubject.value;
    const typeKey = normalize(entityType);
    const entitiesForType = result.items.reduce<
      Readonly<Record<string, BaseEntity>>
    >((entities, entity) => {
      const idKey = normalize(entity.id);
      const existing = entities[idKey];

      return {
        ...entities,
        [idKey]: existing ? { ...existing, ...entity } : entity,
      };
    }, state.entities[typeKey] ?? {});

    this.stateSubject.next({
      entities: {
        ...state.entities,
        [typeKey]: entitiesForType,
      },
      searches: {
        ...state.searches,
        [queryKey]: {
          entityType: typeKey,
          itemIds: result.items.map(entity => normalize(entity.id)),
          page: result.page,
          pageSize: result.pageSize,
          totalCount: result.totalCount,
          totalPages: result.totalPages,
          hasPreviousPage: result.hasPreviousPage,
          hasNextPage: result.hasNextPage,
        },
      },
    });
  }

  remove(entityType: string, id: string): void {
    const state = this.stateSubject.value;
    const typeKey = normalize(entityType);
    const idKey = normalize(id);
    const entitiesForType = { ...(state.entities[typeKey] ?? {}) };
    delete entitiesForType[idKey];

    const searches = Object.fromEntries(
      Object.entries(state.searches).map(([queryKey, entry]) => {
        if (entry.entityType !== typeKey) return [queryKey, entry];

        const itemIds = entry.itemIds.filter(itemId => itemId !== idKey);
        if (itemIds.length === entry.itemIds.length) {
          return [queryKey, entry];
        }

        const totalCount = Math.max(0, entry.totalCount - 1);
        const totalPages =
          totalCount === 0
            ? 0
            : Math.ceil(totalCount / entry.pageSize);

        return [
          queryKey,
          {
            ...entry,
            itemIds,
            totalCount,
            totalPages,
            hasNextPage: entry.page < totalPages,
          },
        ];
      }),
    );

    this.stateSubject.next({
      entities: {
        ...state.entities,
        [typeKey]: entitiesForType,
      },
      searches,
    });
  }

  reset(): void {
    this.stateSubject.next(initialState);
  }
}

function equalPagedResult<T extends BaseEntity>(
  left: PagedResult<T> | undefined,
  right: PagedResult<T> | undefined,
): boolean {
  if (left === right) return true;
  if (!left || !right) return false;

  return (
    left.page === right.page &&
    left.pageSize === right.pageSize &&
    left.totalCount === right.totalCount &&
    left.totalPages === right.totalPages &&
    left.hasPreviousPage === right.hasPreviousPage &&
    left.hasNextPage === right.hasNextPage &&
    left.items.length === right.items.length &&
    left.items.every((item, index) => item === right.items[index])
  );
}
