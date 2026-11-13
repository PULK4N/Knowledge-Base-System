import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { REVIEW_LEVELS, ReviewLevel } from '../../policies/data-access/policy.models';
import { MarkdownContentComponent } from '../../skills/ui/markdown-content.component';
import { FeatureReviewer, PlanReviewAction } from '../data-access/feature.models';
import { ScrollIntoViewDirective } from './scroll-into-view.directive';
import { itemAnchor } from './specification-origin';

/** A plan's own reviewers, run with the feature's and copied to the feature when it completes. */
@Component({
  selector: 'app-plan-reviewers',
  imports: [DatePipe, FormsModule, MarkdownContentComponent, ScrollIntoViewDirective],
  templateUrl: './plan-reviewers.component.html',
  styleUrl: './feature-specification.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlanReviewersComponent {
  readonly reviewers = input.required<readonly FeatureReviewer[]>();
  readonly saving = input(false);
  readonly fragment = input<string | null>(null);

  readonly action = output<PlanReviewAction>();

  protected readonly anchor = itemAnchor;
  protected readonly reviewLevels = REVIEW_LEVELS;
  protected readonly emptyBlocks = [];
  protected readonly adding = signal(false);
  protected readonly editingId = signal<string | null>(null);
  protected readonly confirmingRemovalId = signal<string | null>(null);

  protected add(title: string, description: string, reviewLevel: ReviewLevel): void {
    this.action.emit({
      kind: 'add-reviewer',
      request: { title: title.trim(), description: description.trim(), reviewLevel },
    });
    this.adding.set(false);
  }

  protected update(reviewerId: string, title: string, description: string, reviewLevel: ReviewLevel): void {
    this.action.emit({
      kind: 'update-reviewer',
      request: { reviewerId, title: title.trim(), description: description.trim(), reviewLevel },
    });
    this.editingId.set(null);
  }

  protected remove(reviewerId: string): void {
    this.action.emit({ kind: 'remove-reviewer', reviewerId });
    this.confirmingRemovalId.set(null);
  }
}
