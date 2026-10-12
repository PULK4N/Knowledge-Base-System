import { NgTemplateOutlet } from '@angular/common';
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
import { Feature, SetFeatureBranchRequest } from '../data-access/feature.models';
import { FeatureBranchTreeNode, buildFeatureBranchTree } from './feature-branch-tree';
import { FeaturePlanStatusPipe } from './feature-plan-status';

/**
 * Shows the feature branch with every plan's branches nested in the order the
 * plans were done, and lets the user set the feature branch.
 */
@Component({
  selector: 'app-feature-branch-overview',
  imports: [FeaturePlanStatusPipe, FormsModule, NgTemplateOutlet, RouterLink],
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

  protected readonly tree = computed(() => buildFeatureBranchTree(this.feature()));
  protected readonly collapsed = signal<ReadonlySet<string>>(new Set());
  protected readonly editingBranch = signal(false);

  protected toggle(key: string): void {
    this.collapsed.update(current => {
      const next = new Set(current);
      if (!next.delete(key)) next.add(key);
      return next;
    });
  }

  protected expandAll(): void {
    this.collapsed.set(new Set());
  }

  protected collapseAll(): void {
    this.collapsed.set(new Set(parentKeys(this.tree()).filter(key => key !== 'feature')));
  }

  protected setBranch(branchName: string, description: string): void {
    this.branchSet.emit({
      branchName: branchName.trim(),
      description: description.trim(),
    });
    this.editingBranch.set(false);
  }
}

function parentKeys(node: FeatureBranchTreeNode): string[] {
  return node.children.length === 0
    ? []
    : [node.key, ...node.children.flatMap(parentKeys)];
}
