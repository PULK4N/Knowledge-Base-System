import { Feature, FeaturePlan, ItemOrigin } from '../data-access/feature.models';

/** The plan tab an item lives on. */
export type PlanTab = 'plan' | 'review-notes' | 'reviewers';

/** Where an "origin" link on a feature copy leads. */
export interface OriginLink {
  /** For example: Plan "Extraction" → FR-2. */
  readonly text: string;
  /** Null when the plan was removed; the text is then shown without a link. */
  readonly planId: string | null;
  readonly tab: PlanTab;
  readonly fragment: string;
}

/** The element ID a plan item is rendered with, so links can scroll to it. */
export function itemAnchor(itemId: string): string {
  return `item-${itemId}`;
}

/** Describes where a feature copy came from, or null for items added on the feature. */
export function originLink(
  feature: Feature,
  origin: ItemOrigin | null | undefined,
): OriginLink | null {
  if (!origin) return null;

  const plan = feature.plans.find(item => item.id === origin.planId);
  if (!plan) {
    return {
      text: 'From a removed plan',
      planId: null,
      tab: 'plan',
      fragment: itemAnchor(origin.planItemId),
    };
  }

  const { label, tab } = describePlanItem(plan, origin.planItemId);
  return {
    text: `Plan "${plan.title}" → ${label}`,
    planId: plan.id,
    tab,
    fragment: itemAnchor(origin.planItemId),
  };
}

function describePlanItem(
  plan: FeaturePlan,
  itemId: string,
): { readonly label: string; readonly tab: PlanTab } {
  const specification = plan.specification;
  const requirement = specification.requirements.find(item => item.id === itemId);
  if (requirement) return { label: requirement.label, tab: 'plan' };
  const definitionOfDone = specification.definitionsOfDone.find(item => item.id === itemId);
  if (definitionOfDone) return { label: definitionOfDone.label, tab: 'plan' };
  const script = specification.playwrightScripts.find(item => item.id === itemId);
  if (script) return { label: script.path, tab: 'plan' };
  const reviewNote = plan.reviewNotes.find(item => item.id === itemId);
  if (reviewNote) return { label: reviewNote.title, tab: 'review-notes' };
  const reviewer = plan.reviewers.find(item => item.id === itemId);
  if (reviewer) return { label: reviewer.title, tab: 'reviewers' };
  return { label: 'removed item', tab: 'plan' };
}
