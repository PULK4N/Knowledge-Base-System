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
  SetFeaturePlanBranchSourcesRequest,
  UpdateFeaturePlanBranchRequest,
} from '../data-access/feature.models';
import { sourceFromOptionId, sourceOptionId } from './feature-branch-graph';
import {
  FeatureLinkOption,
  FeatureLinkPickerComponent,
} from './feature-link-picker.component';
import { flattenPlanBranches } from './feature-plan-branches';

type BranchEditor =
  | { readonly mode: 'add'; readonly parentBranchName: string | null }
  | { readonly mode: 'edit'; readonly branchName: string }
  | { readonly mode: 'sources'; readonly branchName: string };

/**
 * Shows a plan's base branches, each with its nested sub-branches as a
 * collapsible tree and the branches it came from, and emits add, update,
 * remove, and source requests for the page to execute.
 */
@Component({
  selector: 'app-feature-plan-branch-tree',
  imports: [FeatureLinkPickerComponent, FormsModule, NgTemplateOutlet],
  templateUrl: './feature-plan-branch-tree.component.html',
  styleUrl: './feature-plan-branch-tree.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FeaturePlanBranchTreeComponent {
  readonly branches = input<readonly FeaturePlanBranch[]>([]);
  /** Branches a base branch may come from: the feature branch and other plans' branches. */
  readonly sourceOptions = input<readonly FeatureLinkOption[]>([]);
  /** Turns a base branch's sources into readable text, including the default. */
  readonly describeSources = input<(branch: FeaturePlanBranch) => string>(
    () => '',
  );
  readonly saving = input(false);

  readonly branchAdded = output<AddFeaturePlanBranchRequest>();
  readonly branchUpdated = output<UpdateFeaturePlanBranchRequest>();
  readonly branchRemoved = output<string>();
  readonly sourcesSet = output<SetFeaturePlanBranchSourcesRequest>();

  /**
   * Branches whose open state differs from the default: base branches start
   * open and every other branch starts collapsed.
   */
  protected readonly toggled = signal<ReadonlySet<string>>(new Set());
  protected readonly editor = signal<BranchEditor | null>(null);
  protected readonly confirmingRemoval = signal<string | null>(null);
  /** Sources picked in the add-base-branch or sources form. */
  protected readonly pickedSourceIds = signal<readonly string[]>([]);

  protected readonly branchCount = computed(
    () => flattenPlanBranches(this.branches()).length,
  );
  private readonly baseBranchNames = computed(
    () => new Set(this.branches().map(branch => branch.name)),
  );
  private readonly parentBranchNames = computed(() =>
    flattenPlanBranches(this.branches())
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
    const baseNames = this.baseBranchNames();
    this.toggled.set(
      new Set(this.parentBranchNames().filter(name => !baseNames.has(name))),
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
    this.pickedSourceIds.set(
      editor.mode === 'sources'
        ? (this.branches()
            .find(branch => branch.name === editor.branchName)
            ?.sources.map(sourceOptionId) ?? [])
        : [],
    );
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
      sources:
        parentBranchName === null
          ? this.pickedSourceIds().map(sourceFromOptionId)
          : [],
    });
    this.editor.set(null);
  }

  protected setSources(branchName: string): void {
    this.sourcesSet.emit({
      branchName,
      sources: this.pickedSourceIds().map(sourceFromOptionId),
    });
    this.editor.set(null);
  }

  protected canRemove(branchName: string): boolean {
    return !this.baseBranchNames().has(branchName) || this.branches().length > 1;
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
    if (this.isOpen(branchName, this.baseBranchNames().has(branchName))) return;
    this.toggle(branchName);
  }

}
