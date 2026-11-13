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
  Feature,
  Requirement,
  RequirementType,
  Specification,
  SpecificationAction,
} from '../data-access/feature.models';
import { OriginLinkComponent } from './origin-link.component';
import { OriginLinkPipe } from './origin-link.pipe';
import { ScrollIntoViewDirective } from './scroll-into-view.directive';
import { itemAnchor } from './specification-origin';
import { REQUIREMENT_TYPES, isWebLink } from './specification-labels';

interface RequirementGroup {
  readonly type: RequirementType;
  readonly label: string;
  readonly requirements: readonly Requirement[];
}

/**
 * Shows the requirements of a plan or of the feature grouped by type, with the
 * definitions of done that fulfil each, and emits add, update and remove changes.
 */
@Component({
  selector: 'app-specification-requirements',
  imports: [FormsModule, OriginLinkComponent, OriginLinkPipe, ScrollIntoViewDirective],
  templateUrl: './specification-requirements.component.html',
  styleUrl: './feature-specification.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SpecificationRequirementsComponent {
  readonly specification = input.required<Specification>();
  /** Set on the feature screen so copies show their origin link. */
  readonly feature = input<Feature | null>(null);
  readonly saving = input(false);
  /** The element ID the page was opened with, to scroll to that item. */
  readonly fragment = input<string | null>(null);

  readonly action = output<SpecificationAction>();

  protected readonly types = REQUIREMENT_TYPES;
  protected readonly anchor = itemAnchor;
  protected readonly isWebLink = isWebLink;
  protected readonly adding = signal(false);
  protected readonly editingId = signal<string | null>(null);
  protected readonly confirmingRemovalId = signal<string | null>(null);

  protected readonly groups = computed<readonly RequirementGroup[]>(() =>
    REQUIREMENT_TYPES.map(type => ({
      type: type.value,
      label: type.label,
      requirements: this.specification().requirements.filter(item => item.type === type.value),
    })).filter(group => group.requirements.length > 0),
  );

  /** Labels of the definitions of done that fulfil each requirement. */
  protected readonly fulfilledBy = computed(() => {
    const result = new Map<string, string[]>();
    for (const definitionOfDone of this.specification().definitionsOfDone)
      for (const requirementId of definitionOfDone.requirementIds)
        result.set(requirementId, [...(result.get(requirementId) ?? []), definitionOfDone.label]);
    return result;
  });

  protected add(type: RequirementType, description: string, externalId: string, source: string): void {
    this.action.emit({
      kind: 'add-requirements',
      requirements: [{ type, description: description.trim(), externalId: externalId.trim(), source: source.trim() }],
    });
    this.adding.set(false);
  }

  protected update(
    id: string,
    type: RequirementType,
    description: string,
    externalId: string,
    source: string,
  ): void {
    this.action.emit({
      kind: 'update-requirements',
      requirements: [{ id, type, description: description.trim(), externalId: externalId.trim(), source: source.trim() }],
    });
    this.editingId.set(null);
  }

  protected remove(id: string): void {
    this.action.emit({ kind: 'remove-requirements', requirementIds: [id] });
    this.confirmingRemovalId.set(null);
  }
}
