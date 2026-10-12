using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

public sealed class FeaturePlanBranchMustExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var (planId, branchName) = payload.EventData switch
        {
            FeaturePlanBranchUpdatedV1 eventData => (eventData.PlanId, eventData.BranchName),
            FeaturePlanBranchRemovedV1 eventData => (eventData.PlanId, eventData.BranchName),
            _ => (default(FeaturePlanId), (string?)null)
        };

        if (branchName is null)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(FeaturePlanBranchMustExistValidator),
                false,
                $"{nameof(FeaturePlanBranchMustExistValidator)} can only validate plan branch update or removal events."
            );
        }

        var exists = ((FeatureStateData)stateData).Plans
            .SingleOrDefault(item => item.Id == planId)
            ?.FindBranch(branchName) is not null;

        return EventValidationResult.FromPayload(
            payload,
            nameof(FeaturePlanBranchMustExistValidator),
            exists,
            exists ? null : $"The plan does not have a branch named '{branchName}'."
        );
    }
}
