using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

/// <summary>
/// Keeps one base branch per plan: a new branch must name an existing parent,
/// except for adding the base branch to a plan created before plans required one.
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
            ({ Branch: not null }, null) => "The plan already has a base branch; name a parent branch.",
            ({ Branch: null }, not null) => "The plan has no base branch yet; add it without a parent.",
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
