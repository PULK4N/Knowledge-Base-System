import { EMPTY_SPECIFICATION, Feature, FeaturePlan } from '../data-access/feature.models';
import { itemAnchor, originLink } from './specification-origin';

const plan = {
  id: 'plan-1',
  title: 'Extraction',
  specification: {
    ...EMPTY_SPECIFICATION,
    requirements: [{ id: 'r1', label: 'FR-2' }],
    playwrightScripts: [{ id: 's1', path: 'e2e/a.spec.ts' }],
  },
  reviewNotes: [{ id: 'n1', title: 'Retries are intended' }],
  reviewers: [],
} as unknown as FeaturePlan;
const feature = { id: 'feature-1', plans: [plan] } as unknown as Feature;

describe('originLink', () => {
  it.each([
    ['r1', 'Plan "Extraction" → FR-2', 'plan'],
    ['s1', 'Plan "Extraction" → e2e/a.spec.ts', 'plan'],
    ['n1', 'Plan "Extraction" → Retries are intended', 'review-notes'],
    ['gone', 'Plan "Extraction" → removed item', 'plan'],
  ] as const)('describes origin %s', (itemId, text, tab) => {
    expect(originLink(feature, { planId: 'plan-1', planItemId: itemId })).toEqual({
      text,
      planId: 'plan-1',
      tab,
      fragment: itemAnchor(itemId),
    });
  });

  it('shows a removed plan without a link and items added on the feature without origin', () => {
    expect(originLink(feature, { planId: 'missing', planItemId: 'r1' })?.planId).toBeNull();
    expect(originLink(feature, null)).toBeNull();
  });
});
