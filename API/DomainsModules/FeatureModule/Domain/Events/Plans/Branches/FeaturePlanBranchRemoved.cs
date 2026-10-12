using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeaturePlanBranchRemoved : IEvent;

/// <summary>
/// Removes a sub-branch together with its own sub-branches. The base branch is never removed.
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
        plan.FindParentBranch(branchName)!.SubBranches
            .RemoveAll(branch => branch.Name == branchName);
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
