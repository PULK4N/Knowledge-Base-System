import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';
import { outputFromObservable } from '@angular/core/rxjs-interop';
import { Subject, debounceTime, groupBy, mergeMap } from 'rxjs';
import { ListControlOption } from '../list-controls/list-controls.component';

export interface ListFilterChange {
  readonly key: string;
  readonly value: string;
}

interface ListFilterBase {
  readonly key: string;
  readonly label: string;
  readonly value: string;
  readonly disabled?: boolean;
}

export interface ListTextFilter extends ListFilterBase {
  readonly kind: 'text';
  readonly type?: 'text' | 'number' | 'date';
  readonly placeholder?: string;
  readonly maxLength?: number;
  readonly min?: number;
  readonly max?: number;
  readonly step?: number;
}

export interface ListSelectFilter extends ListFilterBase {
  readonly kind: 'select';
  readonly options: readonly ListControlOption[];
}

export type ListFilter = ListTextFilter | ListSelectFilter;

@Component({
  selector: 'app-list-filters',
  templateUrl: './list-filters.component.html',
  styleUrl: './list-filters.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ListFiltersComponent {
  readonly filters = input.required<readonly ListFilter[]>();

  private readonly textRequests = new Subject<ListFilterChange>();

  readonly textFilterChanged = outputFromObservable(
    this.textRequests.pipe(
      groupBy(change => change.key),
      mergeMap(changes => changes.pipe(debounceTime(250))),
    ),
  );
  readonly selectFilterChanged = output<ListFilterChange>();

  protected requestText(key: string, value: string): void {
    this.textRequests.next({ key, value });
  }

  protected requestSelection(key: string, value: string): void {
    this.selectFilterChanged.emit({ key, value });
  }
}
