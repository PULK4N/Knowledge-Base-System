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

        var (planId, branchName) = target.Value;
        var plan = ((FeatureStateData)stateData).Plans
            .SingleOrDefault(item => item.Id == planId);
        var taken = branchName is not null
            && plan?.FindBranch(branchName) is not null;

        return EventValidationResult.FromPayload(
            payload,
            nameof(FeaturePlanBranchNameMustBeAvailableValidator),
            !taken,
            taken ? $"The plan already has a branch named '{branchName}'." : null
        );
    }
}
