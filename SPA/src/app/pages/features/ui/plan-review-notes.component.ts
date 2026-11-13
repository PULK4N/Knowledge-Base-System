import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MarkdownContentComponent } from '../../skills/ui/markdown-content.component';
import { FeatureReviewNote, PlanReviewAction } from '../data-access/feature.models';
import { ScrollIntoViewDirective } from './scroll-into-view.directive';
import { itemAnchor } from './specification-origin';

/** A plan's own review notes, used in its reviews and copied to the feature when it completes. */
@Component({
  selector: 'app-plan-review-notes',
  imports: [DatePipe, FormsModule, MarkdownContentComponent, ScrollIntoViewDirective],
  templateUrl: './plan-review-notes.component.html',
  styleUrl: './feature-specification.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlanReviewNotesComponent {
  readonly reviewNotes = input.required<readonly FeatureReviewNote[]>();
  readonly saving = input(false);
  readonly fragment = input<string | null>(null);

  readonly action = output<PlanReviewAction>();

  protected readonly anchor = itemAnchor;
  protected readonly emptyBlocks = [];
  protected readonly adding = signal(false);
  protected readonly editingId = signal<string | null>(null);
  protected readonly confirmingRemovalId = signal<string | null>(null);

  protected add(title: string, content: string): void {
    this.action.emit({ kind: 'add-review-note', request: { title: title.trim(), content: content.trim() } });
    this.adding.set(false);
  }

  protected update(reviewNoteId: string, title: string, content: string): void {
    this.action.emit({
      kind: 'update-review-note',
      request: { reviewNoteId, title: title.trim(), content: content.trim() },
    });
    this.editingId.set(null);
  }

  protected remove(reviewNoteId: string): void {
    this.action.emit({ kind: 'remove-review-note', reviewNoteId });
    this.confirmingRemovalId.set(null);
  }
}
