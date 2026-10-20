import {
  Feature,
  FeatureBranchGraphNode,
  FeaturePlan,
  FeaturePlanStatus,
} from '../data-access/feature.models';
import {
  branchSourceOptions,
  describeSources,
  previousPlanOptions,
  sourceFromOptionId,
  sourceOptionId,
  toBranchGraphLines,
  toBranchGraphMermaid,
} from './feature-branch-graph';

function node(
  key: string,
  planTitle: string | null,
  fromKeys: readonly string[],
  isBaseBranch = true,
  planStatus: FeaturePlanStatus = 'InProgress',
): FeatureBranchGraphNode {
  const [planId, name] = key === 'feature' ? [null, 'main'] : key.split(':');

  return {
    key,
    name,
    description: '',
    planId,
    planTitle,
    planStatus: planId ? planStatus : null,
    isBaseBranch: planId ? isBaseBranch : false,
    fromKeys,
  };
}

function feature(
  branchGraph: readonly FeatureBranchGraphNode[],
  plans: readonly FeaturePlan[] = [],
): Feature {
  return {
    branch: { name: 'main', description: '' },
    branchGraph,
    plans,
  } as unknown as Feature;
}

function plan(
  id: string,
  previousPlanId: string | null,
  branchNames: readonly string[],
): FeaturePlan {
  return {
    id,
    title: `Plan ${id}`,
    content: '',
    contentType: 'Markdown',
    additionalInfo: '',
    status: 'NotStarted',
    branches: branchNames.map(name => ({
      name,
      description: '',
      subBranches: [{ name: `${name}-tests`, description: '', subBranches: [], sources: [] }],
      sources: [],
    })),
    previousPlanId,
    recordIds: [],
    createdAt: '2026-10-01T00:00:00Z',
    updatedAt: '2026-10-01T00:00:00Z',
  };
}

// main ──► x1-api ──┐
//    └───► x1-web ──┴──► x2
const twoSourcesOfOnePlan = feature([
  node('feature', null, []),
  node('x1:x1-api', 'X1', ['feature']),
  node('x1:x1-web', 'X1', ['feature']),
  node('x2:x2', 'X2', ['x1:x1-api', 'x1:x1-web'], true, 'NotStarted'),
]);

const sourcesOfTwoPlans = feature([
  node('feature', null, []),
  node('x1:x1-api', 'X1', ['feature']),
  node('x3:x3-web', 'X3', ['feature']),
  node('x2:x2', 'X2', ['x1:x1-api', 'x3:x3-web'], true, 'NotStarted'),
]);

describe('toBranchGraphLines', () => {
  it.each([
    [
      'two branches of one plan',
      twoSourcesOfOnePlan,
      ['main <- ', 'x1-api <- main', 'x1-web <- main', 'x2 <- x1-api, x1-web'],
    ],
    [
      'branches of two plans',
      sourcesOfTwoPlans,
      ['main <- ', 'x1-api <- main', 'x3-web <- main', 'x2 <- x1-api, x3-web'],
    ],
  ])('lists each branch with the branches it came from: %s', (_, shown, expected) => {
    expect(
      toBranchGraphLines(shown).map(line => `${line.name} <- ${line.from.join(', ')}`),
    ).toEqual(expected);
  });
});

describe('toBranchGraphMermaid', () => {
  it('draws one arrow from each source, so x2 gets an arrow from x1-api and x1-web', () => {
    expect(toBranchGraphMermaid(twoSourcesOfOnePlan).split('\n')).toEqual([
      'flowchart LR',
      '  b0["main"]:::feature',
      '  subgraph p0["X1 · In progress"]',
      '    b1["x1-api"]:::base',
      '    b2["x1-web"]:::base',
      '  end',
      '  subgraph p1["X2 · Not started"]',
      '    b3["x2"]:::base',
      '  end',
      '  b0 --> b1',
      '  b0 --> b2',
      '  b1 --> b3',
      '  b2 --> b3',
      '  classDef feature font-weight:bold',
      '  classDef base font-weight:bold',
    ]);
  });

  it('draws arrows from branches of different plans', () => {
    const lines = toBranchGraphMermaid(sourcesOfTwoPlans).split('\n');

    expect(lines).toContain('  b1 --> b3');
    expect(lines).toContain('  b2 --> b3');
    expect(lines).toContain('  subgraph p1["X3 · In progress"]');
  });

  it('quotes branch names so brackets and quotes stay text', () => {
    const quoted = feature([
      node('feature', null, []),
      { ...node('x1:x', 'Plan "A" [1]', ['feature']), name: 'fix/"b"' },
    ]);

    expect(toBranchGraphMermaid(quoted)).toContain('b1["fix/#quot;b#quot;"]');
    expect(toBranchGraphMermaid(quoted)).toContain('subgraph p0["Plan #quot;A#quot; [1] · In progress"]');
  });
});

describe('branchSourceOptions', () => {
  it('offers the feature branch and every branch of other plans', () => {
    const shown = feature([], [plan('x1', null, ['x1-api']), plan('x2', 'x1', ['x2'])]);

    expect(branchSourceOptions(shown, 'x2').map(option => option.id)).toEqual([
      'feature',
      'x1:x1-api',
      'x1:x1-api-tests',
    ]);
  });

  it.each([
    [{ planId: null, branchName: '' }],
    [{ planId: 'x1', branchName: 'feature/x1:api' }],
  ])('turns a source into an option ID and back: %j', source => {
    expect(sourceFromOptionId(sourceOptionId(source))).toEqual(source);
  });

  it.each([
    [[], 'previous plan or feature branch (default)'],
    [[{ planId: 'x1', branchName: 'x1-api' }, { planId: null, branchName: '' }], 'x1-api, main'],
  ])('describes sources %j as %j', (sources, expected) => {
    expect(describeSources(sources, feature([]))).toBe(expected);
  });
});

describe('previousPlanOptions', () => {
  const plans = [
    plan('retry', 'impl', ['feature/retry']),
    plan('impl', 'research', ['feature/impl']),
    plan('research', null, ['feature/research']),
    plan('legacy', 'missing', []),
  ];

  it.each([
    [null, ['retry', 'impl', 'research', 'legacy']],
    ['impl', ['research', 'legacy']],
    ['research', ['legacy']],
  ])('for %s offers only plans that do not come after it', (planId, expected) => {
    expect(previousPlanOptions(plans, planId).map(item => item.id)).toEqual(expected);
  });
});
