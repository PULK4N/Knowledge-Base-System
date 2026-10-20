using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeaturePlanBranchAdded : IEvent;

/// <summary>
/// Adds a sub-branch under its parent. Without a parent it adds another base
/// branch to the plan, or the first one to a plan created before plans required one.
/// </summary>
public readonly record struct FeaturePlanBranchAddedV1(
    FeaturePlanId PlanId,
    string BranchName,
    string Description,
    string? ParentBranchName,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeaturePlanBranchAdded
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        var planId = PlanId;
        var plan = state.Plans.Single(item => item.Id == planId);
        var branch = new FeaturePlanBranch
        {
            Name = BranchName,
            Description = Description
        };

        if (ParentBranchName is null)
            plan.Branches.Add(branch);
        else
            plan.FindBranch(ParentBranchName)!.SubBranches.Add(branch);

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
