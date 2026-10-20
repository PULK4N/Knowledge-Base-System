import {
  Feature,
  FeaturePlan,
  FeaturePlanBranch,
} from '../data-access/feature.models';

export interface FeatureBranchTreeNode {
  readonly key: string;
  readonly name: string | null;
  readonly description: string;
  /** Set on a plan's base branch node. */
  readonly plan: FeaturePlan | null;
  readonly children: readonly FeatureBranchTreeNode[];
}

/**
 * Builds one tree for a feature: the feature branch at the root, each plan's base
 * branch under the base branch of the plan done before it (or under the feature
 * branch), and each plan's own sub-branches beneath its base branch. Branches of
 * completed plans are already merged, so they are left out and the plans after
 * them move up: main <- x1 <- x2 shows as main <- x2 once x1 is completed.
 */
export function buildFeatureBranchTree(feature: Feature): FeatureBranchTreeNode {
  const planIds = new Set(feature.plans.map(plan => plan.id));
  const plans = [...feature.plans].sort((left, right) =>
    left.createdAt.localeCompare(right.createdAt),
  );
  const followersOf = (previousPlanId: string | null) =>
    plans.filter(plan => {
      const previous =
        plan.previousPlanId && planIds.has(plan.previousPlanId)
          ? plan.previousPlanId
          : null;
      return previous === previousPlanId;
    });
  // A plan without a base branch, or a completed plan, is left out; its
  // followers move up to where it would be.
  const planNodes = (plan: FeaturePlan): FeatureBranchTreeNode[] => {
    const followers = followersOf(plan.id).flatMap(planNodes);

    return plan.branch && plan.status !== 'Completed'
      ? [
          {
            key: `plan:${plan.id}`,
            name: plan.branch.name,
            description: plan.branch.description,
            plan,
            children: [
              ...plan.branch.subBranches.map(branch =>
                subBranchNode(plan.id, branch),
              ),
              ...followers,
            ],
          },
        ]
      : followers;
  };

  return {
    key: 'feature',
    name: feature.branch?.name ?? null,
    description: feature.branch?.description ?? '',
    plan: null,
    children: followersOf(null).flatMap(planNodes),
  };
}

function subBranchNode(
  planId: string,
  branch: FeaturePlanBranch,
): FeatureBranchTreeNode {
  return {
    key: `branch:${planId}:${branch.name}`,
    name: branch.name,
    description: branch.description,
    plan: null,
    children: branch.subBranches.map(subBranch =>
      subBranchNode(planId, subBranch),
    ),
  };
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
