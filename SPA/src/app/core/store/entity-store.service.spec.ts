import { firstValueFrom } from 'rxjs';
import { EntityStore, PagedResult } from './entity-store.service';

interface TestEntity {
  readonly id: string;
  readonly name: string;
  readonly description?: string;
}

function page(items: readonly TestEntity[]): PagedResult<TestEntity> {
  return {
    items,
    page: 1,
    pageSize: 5,
    totalCount: items.length,
    totalPages: items.length > 0 ? 1 : 0,
    hasPreviousPage: false,
    hasNextPage: false,
  };
}

describe('EntityStore', () => {
  let store: EntityStore;

  beforeEach(() => {
    store = new EntityStore();
  });

  it('normalizes IDs consistently when storing and selecting entities', async () => {
    store.upsert('Skill', { id: ' ABC-123 ', name: 'Entity store' });

    const entity = await firstValueFrom(
      store.entity$<TestEntity>(' skill ', 'abc-123'),
    );

    expect(entity?.name).toBe('Entity store');
  });

  it('populates the detail cache when replacing a search result', async () => {
    const summary = { id: 'skill-1', name: 'Angular' };

    store.replaceSearch('skills:1', 'skill', page([summary]));

    const entity = await firstValueFrom(
      store.entity$<TestEntity>('skill', summary.id),
    );
    expect(entity).toBe(summary);
  });

  it('patches every cached search containing an upserted entity', async () => {
    const summary = { id: 'skill-1', name: 'Angular' };
    const unchanged = { id: 'skill-2', name: 'Event sourcing' };
    store.replaceSearch('skills:all', 'skill', page([summary, unchanged]));
    store.replaceSearch('skills:angular', 'skill', page([summary]));

    const detail = {
      ...summary,
      description: 'Observable-first Angular architecture',
    };
    store.upsert('skill', detail);

    const all = await firstValueFrom(
      store.search$<TestEntity>('skills:all'),
    );
    const angular = await firstValueFrom(
      store.search$<TestEntity>('skills:angular'),
    );

    expect(all?.items[0]).toBe(detail);
    expect(all?.items[1]).toBe(unchanged);
    expect(angular?.items[0]).toBe(detail);
  });

  it('does not insert an upserted entity into unrelated cached searches', async () => {
    const existing = { id: 'skill-1', name: 'Angular' };
    store.replaceSearch('skills:angular', 'skill', page([existing]));

    store.upsert('skill', { id: 'skill-2', name: 'Policies' });

    const result = await firstValueFrom(
      store.search$<TestEntity>('skills:angular'),
    );
    expect(result?.items).toEqual([existing]);
  });

  it('preserves richer detail fields when a summary search is refreshed', async () => {
    const detail = {
      id: 'skill-1',
      name: 'Angular',
      description: 'Observable-first architecture',
    };
    store.upsert('skill', detail);

    store.replaceSearch(
      'skills:all',
      'skill',
      page([{ id: detail.id, name: 'Angular 22' }]),
    );

    const selectedDetail = await firstValueFrom(
      store.entity$<TestEntity>('skill', detail.id),
    );
    const search = await firstValueFrom(
      store.search$<TestEntity>('skills:all'),
    );

    expect(selectedDetail).toEqual({
      ...detail,
      name: 'Angular 22',
    });
    expect(search?.items[0]).toBe(selectedDetail);
  });

  it('removes an entity from detail state and every cached search', async () => {
    const removed = { id: 'skill-1', name: 'Angular' };
    const retained = { id: 'skill-2', name: 'Event sourcing' };
    store.replaceSearch('skills:all', 'skill', page([removed, retained]));
    store.replaceSearch('skills:angular', 'skill', page([removed]));

    store.remove('skill', 'SKILL-1');

    const detail = await firstValueFrom(
      store.entity$<TestEntity>('skill', removed.id),
    );
    const all = await firstValueFrom(
      store.search$<TestEntity>('skills:all'),
    );
    const angular = await firstValueFrom(
      store.search$<TestEntity>('skills:angular'),
    );

    expect(detail).toBeUndefined();
    expect(all?.items).toEqual([retained]);
    expect(all?.totalCount).toBe(1);
    expect(angular?.items).toEqual([]);
    expect(angular?.totalCount).toBe(0);
    expect(angular?.totalPages).toBe(0);
  });
});
