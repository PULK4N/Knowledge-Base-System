using FeatureModule.Application.DTOs;
using FeatureModule.Domain;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Services;

public static class FeatureReviewContextResolver
{
    public static FeatureReviewContextDto? Resolve(FeatureStateData state, Guid projectId, string branchName)
    {
        if (state.IsDeleted || state.ProjectId.Value != projectId)
            return null;
        var plans = state.Plans.Where(plan => plan.FindBranch(branchName) is not null).ToList();
        if (plans.Count == 0)
            return null;
        if (plans.Count != 1)
            throw new InvalidOperationException($"Branch '{branchName}' belongs to several plans; fix its KB branch association.");
        var plan = plans[0];
        var baseBranch = plan.FindBaseBranch(branchName);
        if (baseBranch is not null)
        {
            foreach (var recordedSource in baseBranch.Sources.Where(value => value.PlanId is not null))
            {
                if (state.Plans.SingleOrDefault(value => value.Id == recordedSource.PlanId)?.FindBranch(recordedSource.BranchName) is null)
                    throw new InvalidOperationException($"The recorded origin of '{branchName}' is missing from the KB.");
            }
            if (baseBranch.Sources.Count == 0)
            {
                var visited = new HashSet<FeaturePlanId> { plan.Id };
                var previousId = plan.PreviousPlanId;
                while (previousId is { } id)
                {
                    var previous = state.Plans.SingleOrDefault(value => value.Id == id);
                    if (previous is null || !visited.Add(id))
                        throw new InvalidOperationException($"The previous-plan origin of '{branchName}' is invalid.");
                    if (previous.Branches.Count > 0)
                        break;
                    previousId = previous.PreviousPlanId;
                }
            }
        }
        var edge = FeatureBranchGraph.Build(state.Plans)
            .Single(value => value.Branch.PlanId == plan.Id && value.Branch.BranchName == branchName);
        if (edge.From.Count != 1)
            throw new InvalidOperationException($"Branch '{branchName}' needs exactly one recorded origin for code review.");
        var source = edge.From[0];
        var origin = source.PlanId is null ? state.Branch?.Name : source.BranchName;
        if (string.IsNullOrWhiteSpace(origin))
            throw new InvalidOperationException($"The origin of '{branchName}' has no branch name in the KB.");
        return new(new(state.Id.Value, state.ProjectId.Value, state.Name, state.Summary, state.Status),
            FeaturePlanDto.FromModel(plan), origin,
            state.ReviewNotes.Select(FeatureReviewNoteDto.FromModel).ToList(),
            state.Reviewers.Select(FeatureReviewerDto.FromModel).ToList());
    }
}
