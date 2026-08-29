import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { PaginationComponent } from './pagination.component';

@Component({
  imports: [PaginationComponent],
  template: `
    <app-pagination
      [page]="1"
      [pageSize]="pageSize()"
      [pageSizes]="[10, 25, 50]"
      [totalCount]="120"
      [totalPages]="3"
      [hasPreviousPage]="false"
      [hasNextPage]="true"
      (pageSizeRequested)="requestedPageSizes.push($event)"
    />
  `,
})
class PaginationHost {
  readonly pageSize = signal(50);
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
});
