using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeaturePlanBranchRemoved : IEvent;

/// <summary>
/// Removes a branch together with its own sub-branches. A base branch can be
/// removed while the plan keeps another one. Other plans' base branches stop
/// listing the removed branches as their sources.
/// </summary>
public readonly record struct FeaturePlanBranchRemovedV1(
    FeaturePlanId PlanId,
    string BranchName,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeaturePlanBranchRemoved
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        var planId = PlanId;
        var plan = state.Plans.Single(item => item.Id == planId);
        var branchName = BranchName;
        var removedNames = plan.FindBranch(branchName)!
            .SelfAndDescendants()
            .Select(branch => branch.Name)
            .ToHashSet();
        if (plan.FindBaseBranch(branchName) is { } baseBranch)
            plan.Branches.Remove(baseBranch);
        else
            plan.FindParentBranch(branchName)!.SubBranches
                .RemoveAll(branch => branch.Name == branchName);
        FeaturePlanBranchSources.Remove(
            state,
            source => source.PlanId == planId
                && removedNames.Contains(source.BranchName)
        );
        plan.UpdatedAt = eventExecutionInfo.Timestamp;
        state.MemoryHistory.Add(
            new MemoryHistoryRecord(
                eventExecutionInfo.EventName,
                eventExecutionInfo.Timestamp,
                MemoryAggregateId
            )
        );
        return state;
    }
}
