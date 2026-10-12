import { FeaturePlanBranch } from '../data-access/feature.models';

/** Lists a branch and all of its nested sub-branches, parents first. */
export function flattenBranches(
  branch: FeaturePlanBranch | null,
): readonly FeaturePlanBranch[] {
  return branch
    ? [branch, ...branch.subBranches.flatMap(subBranch => flattenBranches(subBranch))]
    : [];
}
