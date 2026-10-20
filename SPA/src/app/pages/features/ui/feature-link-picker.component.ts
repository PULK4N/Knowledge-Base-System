import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';

export interface FeatureLinkOption {
  readonly id: string;
  readonly label: string;
  /** Short context shown after the label, such as a record type. */
  readonly detail: string;
}

/**
 * Checkbox list for choosing linked items, such as the records of a plan or
 * the plans of a new record. It keeps no state; the parent owns the selection.
 */
@Component({
  selector: 'app-feature-link-picker',
  templateUrl: './feature-link-picker.component.html',
  styleUrl: './feature-link-picker.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FeatureLinkPickerComponent {
  readonly legend = input.required<string>();
  readonly options = input.required<readonly FeatureLinkOption[]>();
  readonly selectedIds = input.required<readonly string[]>();
  readonly emptyMessage = input('Nothing to link yet.');
  readonly selectedIdsChange = output<readonly string[]>();

  protected toggle(id: string, selected: boolean): void {
    const others = this.selectedIds().filter(selectedId => selectedId !== id);

    this.selectedIdsChange.emit(selected ? [...others, id] : others);
  }
}
