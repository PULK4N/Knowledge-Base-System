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
  readonly result: PagedResult<BaseEntity>;
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
      map(
        state =>
          state.searches[queryKey]?.result as PagedResult<T> | undefined,
      ),
      distinctUntilChanged(),
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
    const searches = Object.fromEntries(
      Object.entries(state.searches).map(([queryKey, entry]) => [
        queryKey,
        entry.entityType !== typeKey
          ? entry
          : {
              ...entry,
              result: {
                ...entry.result,
                items: entry.result.items.map(item =>
                  normalize(item.id) === idKey ? entity : item,
                ),
              },
            },
      ]),
    );

    this.stateSubject.next({
      entities: {
        ...state.entities,
        [typeKey]: entitiesForType,
      },
      searches,
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
    >(
      (entities, entity) => ({
        ...entities,
        [normalize(entity.id)]: entity,
      }),
      state.entities[typeKey] ?? {},
    );

    this.stateSubject.next({
      entities: {
        ...state.entities,
        [typeKey]: entitiesForType,
      },
      searches: {
        ...state.searches,
        [queryKey]: {
          entityType: typeKey,
          result,
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

        const items = entry.result.items.filter(
          item => normalize(item.id) !== idKey,
        );
        if (items.length === entry.result.items.length) {
          return [queryKey, entry];
        }

        const totalCount = Math.max(0, entry.result.totalCount - 1);
        const totalPages =
          totalCount === 0
            ? 0
            : Math.ceil(totalCount / entry.result.pageSize);

        return [
          queryKey,
          {
            ...entry,
            result: {
              ...entry.result,
              items,
              totalCount,
              totalPages,
              hasNextPage: entry.result.page < totalPages,
            },
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
