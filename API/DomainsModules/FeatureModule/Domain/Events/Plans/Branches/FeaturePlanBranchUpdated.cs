using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeaturePlanBranchUpdated : IEvent;

/// <summary>
/// Renames a branch and replaces its description; its sub-branches stay nested under it.
/// </summary>
public readonly record struct FeaturePlanBranchUpdatedV1(
    FeaturePlanId PlanId,
    string BranchName,
    string NewBranchName,
    string Description,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeaturePlanBranchUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        var planId = PlanId;
        var plan = state.Plans.Single(item => item.Id == planId);
        var branch = plan.FindBranch(BranchName)!;
        branch.Name = NewBranchName;
        branch.Description = Description;
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
