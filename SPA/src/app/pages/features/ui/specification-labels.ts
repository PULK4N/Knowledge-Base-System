import {
  DefinitionOfDoneState,
  RequirementType,
  Specification,
} from '../data-access/feature.models';
import { FeatureLinkOption } from './feature-link-picker.component';

export const REQUIREMENT_TYPES: readonly { readonly value: RequirementType; readonly label: string }[] = [
  { value: 'Business', label: 'Business (BR)' },
  { value: 'Functional', label: 'Functional (FR)' },
  { value: 'NonFunctional', label: 'Non-functional (NFR)' },
  { value: 'Technical', label: 'Technical (TR)' },
];

export const DEFINITION_OF_DONE_STATES: readonly {
  readonly value: DefinitionOfDoneState;
  readonly label: string;
}[] = [
  { value: 'NotConfirmed', label: 'Not confirmed' },
  { value: 'Confirmed', label: 'Confirmed' },
  { value: 'Failed', label: 'Failed' },
  { value: 'Postponed', label: 'Postponed' },
];

export function stateLabel(state: DefinitionOfDoneState): string {
  return DEFINITION_OF_DONE_STATES.find(item => item.value === state)?.label ?? state;
}

/** Labels of the given IDs in the order the IDs are listed; unknown IDs are left out. */
export function labelsOf(
  ids: readonly string[],
  items: readonly { readonly id: string; readonly label: string }[],
): readonly string[] {
  const labels = new Map(items.map(item => [item.id, item.label]));
  return ids.map(id => labels.get(id)).filter((label): label is string => !!label);
}

export function requirementOptions(specification: Specification): readonly FeatureLinkOption[] {
  return specification.requirements.map(item => ({
    id: item.id,
    label: item.label,
    detail: truncate(item.description),
  }));
}

export function definitionOfDoneOptions(specification: Specification): readonly FeatureLinkOption[] {
  return specification.definitionsOfDone.map(item => ({
    id: item.id,
    label: item.label,
    detail: truncate(item.description),
  }));
}

/** True when the text looks like a web link, so it can be shown as one. */
export function isWebLink(text: string): boolean {
  return /^https?:\/\//i.test(text.trim());
}

function truncate(text: string): string {
  return text.length > 70 ? `${text.slice(0, 67)}…` : text;
}
