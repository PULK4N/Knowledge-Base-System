import { TestBed } from '@angular/core/testing';
import { ListControlsComponent } from './list-controls.component';

describe('ListControlsComponent', () => {
  it('debounces search intent without retaining a second writable search state', () => {
    vi.useFakeTimers();
    const fixture = TestBed.createComponent(ListControlsComponent);
    fixture.componentRef.setInput('sortBy', 'name');
    fixture.componentRef.setInput('sortOptions', [
      { value: 'name', label: 'Name' },
    ]);
    fixture.componentRef.setInput('sortDirection', 'Ascending');
    fixture.detectChanges();

    const searches: string[] = [];
    fixture.componentInstance.searchChanged.subscribe(value =>
      searches.push(value),
    );
    const input = fixture.nativeElement.querySelector(
      'input[type="search"]',
    ) as HTMLInputElement;

    input.value = 'projection';
    input.dispatchEvent(new Event('input'));
    vi.advanceTimersByTime(249);
    expect(searches).toEqual([]);
    vi.advanceTimersByTime(1);
    expect(searches).toEqual(['projection']);

    input.dispatchEvent(new Event('input'));
    vi.advanceTimersByTime(250);
    expect(searches).toEqual(['projection', 'projection']);
    vi.useRealTimers();
  });
});
