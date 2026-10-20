import {
  Feature,
  FeatureBranchGraphNode,
  FeaturePlan,
  FeaturePlanBranchSource,
} from '../data-access/feature.models';
import { flattenBranches } from './feature-plan-branches';
import { featurePlanStatusLabel } from './feature-plan-status';
import { FeatureLinkOption } from './feature-link-picker.component';

export const FEATURE_BRANCH_KEY = 'feature';
const FEATURE_BRANCH_LABEL = 'Feature branch not set';

export interface FeatureBranchGraphLine {
  readonly key: string;
  readonly name: string;
  readonly description: string;
  readonly node: FeatureBranchGraphNode;
  /** Names of the branches this one came from. */
  readonly from: readonly string[];
}

/**
 * One line per branch with the branches it came from, in the order the API
 * sends them: the feature branch first, every branch after its sources.
 * Branches of completed plans are already left out by the API.
 */
export function toBranchGraphLines(
  feature: Feature,
): readonly FeatureBranchGraphLine[] {
  const names = new Map(
    feature.branchGraph.map(node => [node.key, nodeName(node)] as const),
  );

  return feature.branchGraph.map(node => ({
    key: node.key,
    name: nodeName(node),
    description: node.description,
    node,
    from: node.fromKeys.map(key => names.get(key) ?? key),
  }));
}

/**
 * Draws the branch graph as a Mermaid flowchart. Each plan is a box holding
 * its branches, and every arrow points from the branch a branch came from,
 * so a branch started from two branches gets two arrows:
 *
 * ```
 * main ──► x1-api ──┐
 *    └───► x1-web ──┴──► x2
 * ```
 */
export function toBranchGraphMermaid(feature: Feature): string {
  const ids = new Map(
    feature.branchGraph.map((node, index) => [node.key, `b${index}`] as const),
  );
  const lines = ['flowchart LR'];
  const planIds: string[] = [];

  for (const node of feature.branchGraph) {
    if (node.planId === null) {
      lines.push(`  ${ids.get(node.key)}[${label(nodeName(node))}]:::feature`);
    } else if (!planIds.includes(node.planId)) {
      planIds.push(node.planId);
    }
  }

  planIds.forEach((planId, index) => {
    const nodes = feature.branchGraph.filter(node => node.planId === planId);
    const plan = nodes[0];

    lines.push(
      `  subgraph p${index}[${label(`${plan.planTitle ?? ''} · ${featurePlanStatusLabel(plan.planStatus ?? 'NotStarted')}`)}]`,
    );
    for (const node of nodes) {
      lines.push(
        `    ${ids.get(node.key)}[${label(nodeName(node))}]${node.isBaseBranch ? ':::base' : ''}`,
      );
    }
    lines.push('  end');
  });

  for (const node of feature.branchGraph) {
    for (const fromKey of node.fromKeys) {
      const fromId = ids.get(fromKey);
      if (fromId) lines.push(`  ${fromId} --> ${ids.get(node.key)}`);
    }
  }

  lines.push('  classDef feature font-weight:bold');
  lines.push('  classDef base font-weight:bold');

  return lines.join('\n');
}

/** Wraps the Mermaid text in a code fence the Markdown renderer draws. */
export function toBranchGraphMarkdown(feature: Feature): string {
  return `\`\`\`mermaid\n${toBranchGraphMermaid(feature)}\n\`\`\``;
}

/**
 * Branches a base branch of the given plan may come from: the feature branch
 * and every branch of the other plans, completed ones included.
 */
export function branchSourceOptions(
  feature: Feature,
  planId: string,
): readonly FeatureLinkOption[] {
  return [
    {
      id: sourceOptionId({ planId: null, branchName: '' }),
      label: feature.branch?.name ?? 'Feature branch',
      detail: 'feature branch',
    },
    ...feature.plans
      .filter(plan => plan.id !== planId)
      .flatMap(plan =>
        plan.branches
          .flatMap(branch => flattenBranches(branch))
          .map(branch => ({
            id: sourceOptionId({ planId: plan.id, branchName: branch.name }),
            label: branch.name,
            detail: plan.title,
          })),
      ),
  ];
}

export function sourceOptionId(source: FeaturePlanBranchSource): string {
  return source.planId === null
    ? FEATURE_BRANCH_KEY
    : `${source.planId}:${source.branchName}`;
}

export function sourceFromOptionId(id: string): FeaturePlanBranchSource {
  if (id === FEATURE_BRANCH_KEY) return { planId: null, branchName: '' };

  const separator = id.indexOf(':');
  return {
    planId: id.slice(0, separator),
    branchName: id.slice(separator + 1),
  };
}

/** Names the branches a base branch came from, or describes the default. */
export function describeSources(
  sources: readonly FeaturePlanBranchSource[],
  feature: Feature,
): string {
  if (sources.length === 0) return 'previous plan or feature branch (default)';

  return sources
    .map(source =>
      source.planId === null
        ? (feature.branch?.name ?? 'feature branch')
        : source.branchName,
    )
    .join(', ');
}

/** Plans that may be chosen as the previous plan: not the plan itself or any plan after it. */
export function previousPlanOptions(
  plans: readonly FeaturePlan[],
  planId: string | null,
): readonly FeaturePlan[] {
  if (planId === null) return plans;

  const later = new Set<string>([planId]);
  let grew = true;
  while (grew) {
    grew = false;
    for (const plan of plans) {
      if (plan.previousPlanId && later.has(plan.previousPlanId) && !later.has(plan.id)) {
        later.add(plan.id);
        grew = true;
      }
    }
  }

  return plans.filter(plan => !later.has(plan.id));
}

function nodeName(node: FeatureBranchGraphNode): string {
  return node.name ?? FEATURE_BRANCH_LABEL;
}

/** Quotes a Mermaid label so branch names with slashes or brackets stay text. */
function label(text: string): string {
  return `"${text.replaceAll('"', '#quot;')}"`;
}
