import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MarkdownContentComponent } from '../../skills/ui/markdown-content.component';
import { Feature, SetFeatureBranchRequest } from '../data-access/feature.models';
import {
  toBranchGraphLines,
  toBranchGraphMarkdown,
} from './feature-branch-graph';
import { FeaturePlanStatusPipe } from './feature-plan-status';

/**
 * Shows the feature's branches as a graph, because a branch can come from
 * several branches, with a readable list of where each branch came from.
 * Branches of completed plans are left out. Also lets the user set the
 * feature branch.
 */
@Component({
  selector: 'app-feature-branch-overview',
  imports: [
    FeaturePlanStatusPipe,
    FormsModule,
    MarkdownContentComponent,
    RouterLink,
  ],
  templateUrl: './feature-branch-overview.component.html',
  styleUrls: [
    './feature-plan-branch-tree.component.css',
    './feature-branch-overview.component.css',
    './feature-pages.css',
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FeatureBranchOverviewComponent {
  readonly feature = input.required<Feature>();
  readonly saving = input(false);

  readonly branchSet = output<SetFeatureBranchRequest>();

  protected readonly emptyBlocks = [];
  protected readonly lines = computed(() => toBranchGraphLines(this.feature()));
  protected readonly graphMarkdown = computed(() =>
    toBranchGraphMarkdown(this.feature()),
  );
  protected readonly editingBranch = signal(false);

  protected setBranch(branchName: string, description: string): void {
    this.branchSet.emit({
      branchName: branchName.trim(),
      description: description.trim(),
    });
    this.editingBranch.set(false);
  }
}
