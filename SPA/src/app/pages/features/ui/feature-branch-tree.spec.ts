import { Feature, FeaturePlan } from '../data-access/feature.models';
import {
  FeatureBranchTreeNode,
  buildFeatureBranchTree,
  previousPlanOptions,
} from './feature-branch-tree';

function plan(
  id: string,
  previousPlanId: string | null,
  branchName: string | null,
  day: number,
): FeaturePlan {
  return {
    id,
    title: `Plan ${id}`,
    content: '',
    contentType: 'Markdown',
    additionalInfo: '',
    status: 'NotStarted',
    branch: branchName
      ? {
          name: branchName,
          description: '',
          subBranches: [{ name: `${branchName}-api`, description: '', subBranches: [] }],
        }
      : null,
    previousPlanId,
    recordIds: [],
    createdAt: `2026-10-0${day}T00:00:00Z`,
    updatedAt: `2026-10-0${day}T00:00:00Z`,
  };
}

const plans = [
  plan('retry', 'impl', 'feature/retry', 3),
  plan('impl', 'research', 'feature/impl', 2),
  plan('research', null, 'feature/research', 1),
  plan('legacy', 'missing', null, 4),
];

function outline(node: FeatureBranchTreeNode, depth = 0): string[] {
  const label = `${'  '.repeat(depth)}${node.name ?? '-'}${node.plan ? ` [${node.plan.id}]` : ''}`;
  return [label, ...node.children.flatMap(child => outline(child, depth + 1))];
}

describe('buildFeatureBranchTree', () => {
  it('nests each plan under the plan done before it and keeps sub-branches first', () => {
    const tree = buildFeatureBranchTree({
      branch: { name: 'develop', description: '' },
      plans: [...plans, plan('after-legacy', 'legacy', 'feature/after-legacy', 5)],
    } as unknown as Feature);

    expect(outline(tree)).toEqual([
      'develop',
      '  feature/research [research]',
      '    feature/research-api',
      '    feature/impl [impl]',
      '      feature/impl-api',
      '      feature/retry [retry]',
      '        feature/retry-api',
      '  feature/after-legacy [after-legacy]',
      '    feature/after-legacy-api',
    ]);
  });
});

describe('previousPlanOptions', () => {
  it.each([
    [null, ['retry', 'impl', 'research', 'legacy']],
    ['impl', ['research', 'legacy']],
    ['research', ['legacy']],
  ])('for %s offers only plans that do not come after it', (planId, expected) => {
    expect(previousPlanOptions(plans, planId).map(item => item.id)).toEqual(expected);
  });
});
