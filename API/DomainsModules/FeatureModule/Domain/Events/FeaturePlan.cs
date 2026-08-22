using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeaturePlanAdded : IEvent;

/// <summary>
/// Adds a plan and makes it the current plan without removing the previously selected plan.
/// </summary>
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

public interface ICurrentFeaturePlanUpdated : IEvent;

public readonly record struct CurrentFeaturePlanUpdatedV1(
    string Title,
    string Content,
    FeaturePlanContentType ContentType
) : ICurrentFeaturePlanUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        var plan = state.Plans.Single(
            item => item.Id == state.CurrentPlanId!.Value
        );
        plan.Title = Title;
        plan.Content = Content;
        plan.ContentType = ContentType;
        plan.UpdatedAt = eventExecutionInfo.Timestamp;
        return state;
    }
}

public interface ICurrentFeaturePlanChanged : IEvent;

public readonly record struct CurrentFeaturePlanChangedV1(
    FeaturePlanId PlanId
) : ICurrentFeaturePlanChanged
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        state.CurrentPlanId = PlanId;
        return state;
    }
}

public interface IFeaturePlanRemoved : IEvent;

/// <summary>
/// Removes a plan and clears the current selection when that plan was current.
/// </summary>
public readonly record struct FeaturePlanRemovedV1(
    FeaturePlanId PlanId
) : IFeaturePlanRemoved
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        var planId = PlanId;
        var plan = state.Plans.Single(item => item.Id == planId);
        state.Plans.Remove(plan);

        if (state.CurrentPlanId == PlanId)
            state.CurrentPlanId = null;

        return state;
    }
}
