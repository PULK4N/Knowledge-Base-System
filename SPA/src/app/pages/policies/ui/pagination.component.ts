import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
} from '@angular/core';
import { visiblePages } from '../../skills/ui/pagination';

@Component({
  selector: 'app-pagination',
  templateUrl: './pagination.component.html',
  styleUrl: './pagination.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaginationComponent {
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly totalCount = input.required<number>();
  readonly totalPages = input.required<number>();
  readonly hasPreviousPage = input.required<boolean>();
  readonly hasNextPage = input.required<boolean>();
  readonly label = input('Items');
  readonly pageRequested = output<number>();

  protected readonly pages = computed(() =>
    visiblePages(this.totalPages(), this.page()),
  );
  protected readonly firstVisibleItem = computed(
    () => (this.page() - 1) * this.pageSize() + 1,
  );
  protected readonly lastVisibleItem = computed(() =>
    Math.min(this.page() * this.pageSize(), this.totalCount()),
  );

  protected requestPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page()) return;

    this.pageRequested.emit(page);
  }
}
