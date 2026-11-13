import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
  signal,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  DefinitionOfDone,
  DefinitionOfDoneState,
  Feature,
  Specification,
  SpecificationAction,
} from '../data-access/feature.models';
import { FeatureLinkPickerComponent } from './feature-link-picker.component';
import { OriginLinkComponent } from './origin-link.component';
import { OriginLinkPipe } from './origin-link.pipe';
import { ScrollIntoViewDirective } from './scroll-into-view.directive';
import { itemAnchor } from './specification-origin';
import {
  DEFINITION_OF_DONE_STATES,
  labelsOf,
  requirementOptions,
  stateLabel,
} from './specification-labels';

/** A plan definitions of done can move to. */
export interface MoveTargetOption {
  readonly id: string;
  readonly title: string;
}

type DefinitionOfDoneEditor =
  | { readonly mode: 'edit'; readonly id: string }
  | { readonly mode: 'state'; readonly id: string };

/**
 * Shows definitions of done with their state, the requirements they fulfil and
 * the scripts that confirm them, and emits add, update, remove, state and move changes.
 */
@Component({
  selector: 'app-specification-definitions-of-done',
  imports: [
    DatePipe,
    FeatureLinkPickerComponent,
    FormsModule,
    OriginLinkComponent,
    OriginLinkPipe,
    ScrollIntoViewDirective,
  ],
  templateUrl: './specification-definitions-of-done.component.html',
  styleUrl: './feature-specification.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SpecificationDefinitionsOfDoneComponent {
  readonly specification = input.required<Specification>();
  /** Set on the feature screen so copies show their origin link. */
  readonly feature = input<Feature | null>(null);
  readonly saving = input(false);
  readonly fragment = input<string | null>(null);
  /** Plans the definitions of done can move to; empty hides moving. */
  readonly moveTargets = input<readonly MoveTargetOption[]>([]);

  readonly action = output<SpecificationAction>();

  protected readonly states = DEFINITION_OF_DONE_STATES;
  protected readonly anchor = itemAnchor;
  protected readonly stateLabel = stateLabel;
  protected readonly adding = signal(false);
  protected readonly editor = signal<DefinitionOfDoneEditor | null>(null);
  protected readonly confirmingRemovalId = signal<string | null>(null);
  /** Requirements picked in the add or edit form. */
  protected readonly pickedRequirementIds = signal<readonly string[]>([]);
  /** Definitions of done picked for moving. */
  protected readonly selectedIds = signal<readonly string[]>([]);
  protected readonly moveTargetId = signal('');

  protected readonly requirementOptions = computed(() => requirementOptions(this.specification()));
  protected readonly definitionsOfDone = computed(() =>
    [...this.specification().definitionsOfDone].sort((left, right) => left.number - right.number),
  );
  /** Paths of the scripts that confirm each definition of done. */
  protected readonly confirmedBy = computed(() => {
    const result = new Map<string, string[]>();
    for (const script of this.specification().playwrightScripts)
      for (const id of script.definitionOfDoneIds)
        result.set(id, [...(result.get(id) ?? []), script.path]);
    return result;
  });

  protected requirementLabels(definitionOfDone: DefinitionOfDone): readonly string[] {
    return labelsOf(definitionOfDone.requirementIds, this.specification().requirements);
  }

  protected startAdding(): void {
    this.pickedRequirementIds.set([]);
    this.adding.set(!this.adding());
  }

  protected startEditing(definitionOfDone: DefinitionOfDone): void {
    this.pickedRequirementIds.set(definitionOfDone.requirementIds);
    this.editor.set({ mode: 'edit', id: definitionOfDone.id });
  }

  protected isEditing(mode: DefinitionOfDoneEditor['mode'], id: string): boolean {
    const editor = this.editor();
    return editor?.mode === mode && editor.id === id;
  }

  protected toggleSelected(id: string, selected: boolean): void {
    const others = this.selectedIds().filter(item => item !== id);
    this.selectedIds.set(selected ? [...others, id] : others);
  }

  protected add(description: string): void {
    this.action.emit({
      kind: 'add-definitions-of-done',
      definitionsOfDone: [{ description: description.trim(), requirementIds: this.pickedRequirementIds() }],
    });
    this.adding.set(false);
  }

  protected update(id: string, description: string): void {
    this.action.emit({
      kind: 'update-definitions-of-done',
      definitionsOfDone: [{ id, description: description.trim(), requirementIds: this.pickedRequirementIds() }],
    });
    this.editor.set(null);
  }

  protected setState(id: string, state: DefinitionOfDoneState, stateDescription: string): void {
    this.action.emit({
      kind: 'set-definition-of-done-states',
      states: [{ id, state, stateDescription: stateDescription.trim() }],
    });
    this.editor.set(null);
  }

  protected remove(id: string): void {
    this.action.emit({ kind: 'remove-definitions-of-done', definitionOfDoneIds: [id] });
    this.confirmingRemovalId.set(null);
  }

  protected move(): void {
    this.action.emit({
      kind: 'move-definitions-of-done',
      targetPlanId: this.moveTargetId(),
      definitionOfDoneIds: this.selectedIds(),
    });
    this.selectedIds.set([]);
    this.moveTargetId.set('');
  }
}
