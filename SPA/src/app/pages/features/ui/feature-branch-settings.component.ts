import { ChangeDetectionStrategy, Component, OnChanges, SimpleChanges, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Feature, SetFeatureBranchRequest, SetFeatureCompositeBranchRequest } from '../data-access/feature.models';

@Component({
  selector: 'app-feature-branch-settings',
  imports: [FormsModule],
  templateUrl: './feature-branch-settings.component.html',
  styleUrls: ['./feature-plan-branch-tree.component.css', './feature-branch-settings.component.css'],
  host: { '[class.compact]': 'compact()' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FeatureBranchSettingsComponent implements OnChanges {
  readonly feature = input.required<Feature>();
  readonly saving = input(false);
  readonly compact = input(false);
  readonly branchSet = output<SetFeatureBranchRequest>();
  readonly compositeBranchSet = output<SetFeatureCompositeBranchRequest>();

  protected readonly editingBranch = signal(false);
  protected readonly editingCompositeBranch = signal(false);

  ngOnChanges(changes: SimpleChanges): void {
    // Keep edits after a failed save; close when refreshed feature data arrives.
    if (changes['feature']) {
      this.editingBranch.set(false);
      this.editingCompositeBranch.set(false);
    }
  }

  protected setBranch(branchName: string, description: string): void {
    this.branchSet.emit({ branchName: branchName.trim(), description: description.trim() });
  }

  protected setCompositeBranch(branchName: string): void {
    this.compositeBranchSet.emit({ branchName: branchName.trim() });
  }
}
