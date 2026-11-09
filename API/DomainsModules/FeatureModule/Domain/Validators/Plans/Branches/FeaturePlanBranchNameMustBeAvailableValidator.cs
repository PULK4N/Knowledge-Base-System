using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

public sealed class FeaturePlanBranchNameMustBeAvailableValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        // A rename to the branch's own name keeps the name, so nothing new is claimed.
        (FeaturePlanId PlanId, string? BranchName)? target = payload.EventData switch
        {
            FeaturePlanBranchAddedV1 eventData => (eventData.PlanId, eventData.BranchName),
            FeaturePlanBranchUpdatedV1 eventData => (
                eventData.PlanId,
                eventData.NewBranchName == eventData.BranchName ? null : eventData.NewBranchName
            ),
            _ => null
        };

        if (target is null)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(FeaturePlanBranchNameMustBeAvailableValidator),
                false,
                $"{nameof(FeaturePlanBranchNameMustBeAvailableValidator)} can only validate plan branch addition or update events."
            );
        }

        // A branch belongs to one plan only, so every plan of the feature is checked.
        var (planId, branchName) = target.Value;
        var owner = branchName is null
            ? null
            : ((FeatureStateData)stateData).Plans.FirstOrDefault(
                plan => plan.AllBranches().Any(branch => branch.Name == branchName)
            );

        return EventValidationResult.FromPayload(
            payload,
            nameof(FeaturePlanBranchNameMustBeAvailableValidator),
            owner is null,
            owner is null
                ? null
                : owner.Id == planId
                    ? $"The plan already has a branch named '{branchName}'."
                    : $"Plan '{owner.Title}' already has a branch named '{branchName}'. A branch can belong to only one plan."
        );
    }
}
