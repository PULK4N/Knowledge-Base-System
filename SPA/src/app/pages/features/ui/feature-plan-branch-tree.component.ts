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
import {
  AddFeaturePlanBranchRequest,
  FeaturePlanBranch,
  UpdateFeaturePlanBranchRequest,
} from '../data-access/feature.models';
import { flattenBranches } from './feature-plan-branches';

type BranchEditor =
  | { readonly mode: 'add'; readonly parentBranchName: string | null }
  | { readonly mode: 'edit'; readonly branchName: string };

/**
 * Shows a plan's base branch and nested sub-branches as a collapsible tree
 * and emits add, update, and remove requests for the page to execute.
 */
@Component({
  selector: 'app-feature-plan-branch-tree',
  imports: [FormsModule, NgTemplateOutlet],
  templateUrl: './feature-plan-branch-tree.component.html',
  styleUrl: './feature-plan-branch-tree.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FeaturePlanBranchTreeComponent {
  readonly branch = input<FeaturePlanBranch | null>(null);
  readonly saving = input(false);

  readonly branchAdded = output<AddFeaturePlanBranchRequest>();
  readonly branchUpdated = output<UpdateFeaturePlanBranchRequest>();
  readonly branchRemoved = output<string>();

  /**
   * Branches whose open state differs from the default: the base branch starts
   * open and every other branch starts collapsed.
   */
  protected readonly toggled = signal<ReadonlySet<string>>(new Set());
  protected readonly editor = signal<BranchEditor | null>(null);
  protected readonly confirmingRemoval = signal<string | null>(null);

  protected readonly branchCount = computed(
    () => flattenBranches(this.branch()).length,
  );
  private readonly parentBranchNames = computed(() =>
    flattenBranches(this.branch())
      .filter(branch => branch.subBranches.length > 0)
      .map(branch => branch.name),
  );

  protected isOpen(branchName: string, isBase: boolean): boolean {
    return isBase !== this.toggled().has(branchName);
  }

  protected toggle(branchName: string): void {
    this.toggled.update(current => {
      const next = new Set(current);
      if (!next.delete(branchName)) next.add(branchName);
      return next;
    });
  }

  protected expandAll(): void {
    const baseName = this.branch()?.name;
    this.toggled.set(
      new Set(this.parentBranchNames().filter(name => name !== baseName)),
    );
  }

  protected collapseAll(): void {
    this.toggled.set(new Set());
  }

  protected isEditing(mode: BranchEditor['mode'], branchName: string | null): boolean {
    const editor = this.editor();
    if (!editor || editor.mode !== mode) return false;

    return editor.mode === 'add'
      ? editor.parentBranchName === branchName
      : editor.branchName === branchName;
  }

  protected openEditor(editor: BranchEditor): void {
    this.confirmingRemoval.set(null);
    this.editor.set(editor);
  }

  protected addBranch(
    parentBranchName: string | null,
    branchName: string,
    description: string,
  ): void {
    if (parentBranchName !== null) this.open(parentBranchName);
    this.branchAdded.emit({
      branchName: branchName.trim(),
      description: description.trim(),
      parentBranchName,
    });
    this.editor.set(null);
  }

  protected updateBranch(
    branchName: string,
    newBranchName: string,
    description: string,
  ): void {
    this.branchUpdated.emit({
      branchName,
      newBranchName: newBranchName.trim(),
      description: description.trim(),
    });
    this.editor.set(null);
  }

  protected removeBranch(branchName: string): void {
    this.branchRemoved.emit(branchName);
    this.confirmingRemoval.set(null);
  }

  private open(branchName: string): void {
    if (this.isOpen(branchName, branchName === this.branch()?.name)) return;
    this.toggle(branchName);
  }

}
