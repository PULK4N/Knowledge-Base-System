import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { PaginationComponent } from './pagination.component';

@Component({
  imports: [PaginationComponent],
  template: `
    <app-pagination
      [page]="page()"
      [pageSize]="pageSize()"
      [pageSizes]="[10, 25, 50]"
      [totalCount]="120"
      [totalPages]="3"
      [hasPreviousPage]="page() > 1"
      [hasNextPage]="page() < 3"
      (pageRequested)="requestedPages.push($event)"
      (pageSizeRequested)="requestedPageSizes.push($event)"
    />
  `,
})
class PaginationHost {
  readonly page = signal(1);
  readonly pageSize = signal(50);
  readonly requestedPages: number[] = [];
  readonly requestedPageSizes: number[] = [];
}

describe('PaginationComponent', () => {
  it('selects the current page size and reports a change back to it', () => {
    const fixture = TestBed.createComponent(PaginationHost);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    const select = element.querySelector('select') as HTMLSelectElement;

    expect(select.value).toBe('50');

    select.value = '10';
    select.dispatchEvent(new Event('change'));
    expect(fixture.componentInstance.requestedPageSizes).toEqual([10]);

    fixture.componentInstance.pageSize.set(10);
    fixture.detectChanges();
    expect(select.value).toBe('10');
  });

  it.each([
    ['First page', 3, 1],
    ['Last page', 1, 3],
  ])(
    'jumps to the edge page through the %s button',
    (label, currentPage, expectedPage) => {
      const fixture = TestBed.createComponent(PaginationHost);
      fixture.componentInstance.page.set(currentPage);
      fixture.detectChanges();
      const element = fixture.nativeElement as HTMLElement;
      const button = element.querySelector(
        `button[aria-label="${label}"]`,
      ) as HTMLButtonElement;

      expect(button.disabled).toBe(false);

      button.click();
      expect(fixture.componentInstance.requestedPages).toEqual([expectedPage]);
    },
  );

  it.each(['First page', 'Previous page'])(
    'disables the %s button on the first page',
    (label) => {
      const fixture = TestBed.createComponent(PaginationHost);
      fixture.detectChanges();
      const element = fixture.nativeElement as HTMLElement;
      const button = element.querySelector(
        `button[aria-label="${label}"]`,
      ) as HTMLButtonElement;

      expect(button.disabled).toBe(true);
    },
  );
});
