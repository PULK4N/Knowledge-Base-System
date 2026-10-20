using System.Collections.Immutable;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeaturePlanBaseBranchSourcesSet : IEvent;

/// <summary>
/// Replaces the branches a base branch was created from. An empty list goes
/// back to the default: the previous plan's first base branch or the feature branch.
/// </summary>
public readonly record struct FeaturePlanBaseBranchSourcesSetV1(
    FeaturePlanId PlanId,
    string BranchName,
    ImmutableArray<FeaturePlanBranchSource> Sources,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeaturePlanBaseBranchSourcesSet
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        var planId = PlanId;
        var plan = state.Plans.Single(item => item.Id == planId);
        plan.FindBaseBranch(BranchName)!.Sources = Sources.ToList();
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
