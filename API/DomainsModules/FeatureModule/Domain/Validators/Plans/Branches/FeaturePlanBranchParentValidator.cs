using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

/// <summary>
/// A new sub-branch must name an existing parent branch of its plan. A branch
/// without a parent is another base branch and is always allowed.
/// </summary>
public sealed class FeaturePlanBranchParentValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        if (payload.EventData is not FeaturePlanBranchAddedV1 eventData)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(FeaturePlanBranchParentValidator),
                false,
                $"{nameof(FeaturePlanBranchParentValidator)} can only validate {nameof(FeaturePlanBranchAddedV1)} events."
            );
        }

        var plan = ((FeatureStateData)stateData).Plans
            .SingleOrDefault(item => item.Id == eventData.PlanId);
        var error = (plan, eventData.ParentBranchName) switch
        {
            (null, _) => "The feature plan does not exist.",
            (_, { } parentName) when plan.FindBranch(parentName) is null =>
                $"The plan does not have a parent branch named '{parentName}'.",
            _ => null
        };

        return EventValidationResult.FromPayload(
            payload,
            nameof(FeaturePlanBranchParentValidator),
            error is null,
            error
        );
    }
}
