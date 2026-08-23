import { TestBed } from '@angular/core/testing';
import {
  ListFilterChange,
  ListFiltersComponent,
} from './list-filters.component';

describe('ListFiltersComponent', () => {
  it('emits selects immediately and debounces text without owning filter state', () => {
    vi.useFakeTimers();
    const fixture = TestBed.createComponent(ListFiltersComponent);
    fixture.componentRef.setInput('filters', [
      {
        kind: 'text',
        key: 'tag',
        label: 'Tag',
        value: '',
      },
      {
        kind: 'select',
        key: 'hasReferences',
        label: 'References',
        value: '',
        options: [
          { value: '', label: 'Any' },
          { value: 'true', label: 'Has references' },
        ],
      },
    ]);
    fixture.detectChanges();

    const textChanges: ListFilterChange[] = [];
    const selectChanges: ListFilterChange[] = [];
    fixture.componentInstance.textFilterChanged.subscribe(value =>
      textChanges.push(value),
    );
    fixture.componentInstance.selectFilterChanged.subscribe(value =>
      selectChanges.push(value),
    );

    const input = fixture.nativeElement.querySelector(
      'input',
    ) as HTMLInputElement;
    input.value = 'angular';
    input.dispatchEvent(new Event('input'));
    vi.advanceTimersByTime(249);
    expect(textChanges).toEqual([]);
    vi.advanceTimersByTime(1);
    expect(textChanges).toEqual([{ key: 'tag', value: 'angular' }]);

    const select = fixture.nativeElement.querySelector(
      'select',
    ) as HTMLSelectElement;
    select.value = 'true';
    select.dispatchEvent(new Event('change'));
    expect(selectChanges).toEqual([
      { key: 'hasReferences', value: 'true' },
    ]);
    vi.useRealTimers();
  });
});
