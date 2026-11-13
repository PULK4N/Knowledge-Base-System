using FeatureModule.Application.DTOs;
using FeatureModule.Application.Exceptions;
using FeatureModule.Domain;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Services;

public static class FeatureReviewContextResolver
{
    public static FeatureReviewContextDto? Resolve(FeatureStateData state, Guid projectId, string branchName)
    {
        if (state.IsDeleted || state.ProjectId.Value != projectId)
            return null;
        var plans = state.Plans.Where(plan => plan.AllBranches().Any(branch => branch.Name == branchName)).ToList();
        if (plans.Count == 0)
            return null;
        if (plans.Count != 1)
            throw new FeatureReviewContextConflictException(
                $"Branch '{branchName}' is set on several plans of feature '{state.Name}' ({Titles(plans)}). "
                + "Keep it on one plan only.");
        var plan = plans[0];
        if (plan.AllBranches().Count(branch => branch.Name == branchName) > 1)
            throw new FeatureReviewContextConflictException(
                $"Branch '{branchName}' appears more than once in plan '{plan.Title}'. Remove the duplicate branch.");
        var baseBranch = plan.FindBaseBranch(branchName);
        if (baseBranch is not null)
        {
            foreach (var recordedSource in baseBranch.Sources.Where(value => value.PlanId is not null))
            {
                if (state.Plans.SingleOrDefault(value => value.Id == recordedSource.PlanId)?.FindBranch(recordedSource.BranchName) is null)
                    throw new FeatureReviewContextConflictException(
                        $"Branch '{branchName}' in plan '{plan.Title}' was created from '{recordedSource.BranchName}', "
                        + "which no longer exists in the feature. Update the branch source.");
            }
            if (baseBranch.Sources.Count == 0)
            {
                var visited = new HashSet<FeaturePlanId> { plan.Id };
                var previousId = plan.PreviousPlanId;
                while (previousId is { } id)
                {
                    var previous = state.Plans.SingleOrDefault(value => value.Id == id);
                    if (previous is null || !visited.Add(id))
                        throw new FeatureReviewContextConflictException(
                            $"The previous plan of '{plan.Title}' is missing or the previous-plan chain loops back. "
                            + "Fix the previous plan so the origin of branch '" + branchName + "' can be found.");
                    if (previous.Branches.Count > 0)
                        break;
                    previousId = previous.PreviousPlanId;
                }
            }
        }
        var edge = FeatureBranchGraph.Build(state.Plans)
            .Single(value => value.Branch.PlanId == plan.Id && value.Branch.BranchName == branchName);
        if (edge.From.Count != 1)
            throw new FeatureReviewContextConflictException(
                $"Branch '{branchName}' in plan '{plan.Title}' was created from {edge.From.Count} branches. "
                + "Code review needs exactly one origin branch to compare against.");
        var source = edge.From[0];
        var origin = source.PlanId is null ? state.Branch?.Name : source.BranchName;
        if (string.IsNullOrWhiteSpace(origin))
            throw new FeatureReviewContextConflictException(
                $"The origin of branch '{branchName}' has no branch name. Set the branch name of feature '{state.Name}'.");
        return new(new(state.Id.Value, state.ProjectId.Value, state.Name, state.Summary, state.Status),
            FeaturePlanDto.FromModel(plan), origin,
            state.ReviewNotes.Concat(plan.ReviewNotes).Select(FeatureReviewNoteDto.FromModel).ToList(),
            state.Reviewers.Concat(plan.Reviewers).Select(FeatureReviewerDto.FromModel).ToList());
    }

    private static string Titles(List<FeaturePlan> plans) =>
        string.Join(", ", plans.Select(plan => "'" + plan.Title + "'"));
}
