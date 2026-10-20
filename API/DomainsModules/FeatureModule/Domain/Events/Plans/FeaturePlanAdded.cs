using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

// Released plan events still read and clear the legacy current-plan selection during replay.
#pragma warning disable CS0618

public interface IFeaturePlanAdded : IEvent;

/// <summary>
/// Adds a plan and makes it the current plan without removing the previously selected plan.
/// </summary>
[Obsolete("Replay only. New plans use FeaturePlanAddedV4.")]
public readonly record struct FeaturePlanAddedV1(
    FeaturePlanId PlanId,
    string Title,
    string Content,
    FeaturePlanContentType ContentType
) : IFeaturePlanAdded
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        state.Plans.Add(
            new Models.FeaturePlan
            {
                Id = PlanId,
                Title = Title,
                Content = Content,
                ContentType = ContentType,
                CreatedAt = eventExecutionInfo.Timestamp,
                UpdatedAt = eventExecutionInfo.Timestamp
            }
        );
        state.CurrentPlanId = PlanId;
        return state;
    }
}

[Obsolete("Replay only. New plans use FeaturePlanAddedV4.")]
public readonly record struct FeaturePlanAddedV2(
    FeaturePlanId PlanId,
    string Title,
    string Content,
    FeaturePlanContentType ContentType,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeaturePlanAdded
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        state.Plans.Add(
            new Models.FeaturePlan
            {
                Id = PlanId,
                Title = Title,
                Content = Content,
                ContentType = ContentType,
                CreatedAt = eventExecutionInfo.Timestamp,
                UpdatedAt = eventExecutionInfo.Timestamp
            }
        );
        state.CurrentPlanId = PlanId;
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

/// <summary>
/// Adds a plan with its required base branch, without selecting it as a current plan.
/// </summary>
[Obsolete("Replay only. New plans use FeaturePlanAddedV4.")]
public readonly record struct FeaturePlanAddedV3(
    FeaturePlanId PlanId,
    string Title,
    string Content,
    FeaturePlanContentType ContentType,
    string AdditionalInfo,
    FeaturePlanStatus Status,
    string BranchName,
    string BranchDescription,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeaturePlanAdded
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        state.Plans.Add(
            new Models.FeaturePlan
            {
                Id = PlanId,
                Title = Title,
                Content = Content,
                ContentType = ContentType,
                AdditionalInfo = AdditionalInfo,
                Status = Status,
                Branches =
                [
                    new FeaturePlanBranch
                    {
                        Name = BranchName,
                        Description = BranchDescription
                    }
                ],
                CreatedAt = eventExecutionInfo.Timestamp,
                UpdatedAt = eventExecutionInfo.Timestamp
            }
        );
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

/// <summary>
/// Adds a plan with its required base branch and the optional plan done before it.
/// </summary>
public readonly record struct FeaturePlanAddedV4(
    FeaturePlanId PlanId,
    string Title,
    string Content,
    FeaturePlanContentType ContentType,
    string AdditionalInfo,
    FeaturePlanStatus Status,
    string BranchName,
    string BranchDescription,
    FeaturePlanId? PreviousPlanId,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeaturePlanAdded
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        state.Plans.Add(
            new Models.FeaturePlan
            {
                Id = PlanId,
                Title = Title,
                Content = Content,
                ContentType = ContentType,
                AdditionalInfo = AdditionalInfo,
                Status = Status,
                Branches =
                [
                    new FeaturePlanBranch
                    {
                        Name = BranchName,
                        Description = BranchDescription
                    }
                ],
                PreviousPlanId = PreviousPlanId,
                CreatedAt = eventExecutionInfo.Timestamp,
                UpdatedAt = eventExecutionInfo.Timestamp
            }
        );
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
