import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

/** Icon colour family; each list picks the one that matches its entity. */
export type ListRowTone = 'teal' | 'amber' | 'violet' | 'indigo';

/**
 * One list entry in the Projects list style: icon, title, one-line subtitle,
 * metadata and an arrow when the row links somewhere.
 *
 * Project row buttons with the `listRowActions` attribute; they sit on the
 * right of the row. Any other projected content, such as an inline editor or
 * an error, takes the full width under the row.
 */
@Component({
  selector: 'app-list-row',
  imports: [NgTemplateOutlet, RouterLink],
  templateUrl: './list-row.component.html',
  styleUrl: './list-row.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ListRowComponent {
  readonly title = input.required<string>();
  readonly subtitle = input('');
  readonly metadata = input('');
  readonly icon = input('▤');
  readonly tone = input<ListRowTone>('teal');
  readonly link = input<string | readonly string[] | null>(null);
}
