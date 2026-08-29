import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';
import { outputFromObservable } from '@angular/core/rxjs-interop';
import { Subject, debounceTime } from 'rxjs';
import { ListSortDirection } from '../list-state/list-state';

export interface ListControlOption {
  readonly value: string;
  readonly label: string;
}

@Component({
  selector: 'app-list-controls',
  templateUrl: './list-controls.component.html',
  styleUrl: './list-controls.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ListControlsComponent {
  readonly search = input('');
  readonly searchLabel = input('Search items');
  readonly searchPlaceholder = input('Search');
  readonly searchMaxLength = input(500);
  readonly secondarySearch = input<string | null>(null);
  readonly secondarySearchLabel = input('Semantic search');
  readonly secondarySearchPlaceholder = input('Search by meaning');
  readonly secondarySearchMaxLength = input(500);
  readonly filter = input('');
  readonly filterLabel = input('Filter');
  readonly filterOptions = input<readonly ListControlOption[]>([]);
  readonly filterDisabled = input(false);
  readonly sortBy = input.required<string>();
  readonly sortOptions = input.required<readonly ListControlOption[]>();
  readonly sortDirection = input.required<ListSortDirection>();

  private readonly searchRequests = new Subject<string>();
  private readonly secondarySearchRequests = new Subject<string>();

  readonly searchChanged = outputFromObservable(
    this.searchRequests.pipe(debounceTime(250)),
  );
  readonly secondarySearchChanged = outputFromObservable(
    this.secondarySearchRequests.pipe(debounceTime(250)),
  );
  readonly filterChanged = output<string>();
  readonly sortByChanged = output<string>();
  readonly sortDirectionChanged = output<ListSortDirection>();

  protected requestSearch(value: string): void {
    this.searchRequests.next(value);
  }

  protected requestSecondarySearch(value: string): void {
    this.secondarySearchRequests.next(value);
  }

  protected requestFilter(value: string): void {
    this.filterChanged.emit(value);
  }

  protected requestSort(value: string): void {
    this.sortByChanged.emit(value);
  }

  protected requestDirection(value: string): void {
    this.sortDirectionChanged.emit(
      value === 'Descending' ? 'Descending' : 'Ascending',
    );
  }
}
