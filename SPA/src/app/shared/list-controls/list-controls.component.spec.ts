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

  it('renders and independently emits the optional semantic search', () => {
    vi.useFakeTimers();
    const fixture = TestBed.createComponent(ListControlsComponent);
    fixture.componentRef.setInput('secondarySearch', 'existing meaning');
    fixture.componentRef.setInput('sortBy', 'Relevance');
    fixture.componentRef.setInput('sortOptions', [
      { value: 'Relevance', label: 'Relevance' },
    ]);
    fixture.componentRef.setInput('sortDirection', 'Descending');
    fixture.detectChanges();

    const searches: string[] = [];
    fixture.componentInstance.secondarySearchChanged.subscribe(value =>
      searches.push(value),
    );
    const inputs = fixture.nativeElement.querySelectorAll(
      'input[type="search"]',
    ) as NodeListOf<HTMLInputElement>;

    expect(inputs).toHaveLength(2);
    expect(inputs[1].value).toBe('existing meaning');
    inputs[1].value = 'event replay decision';
    inputs[1].dispatchEvent(new Event('input'));
    vi.advanceTimersByTime(250);

    expect(searches).toEqual(['event replay decision']);
    vi.useRealTimers();
  });
});
