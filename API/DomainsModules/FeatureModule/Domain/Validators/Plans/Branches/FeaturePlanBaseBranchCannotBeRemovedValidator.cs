using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

/// <summary>
/// A plan always keeps its base branch; it can be renamed or redescribed but not removed.
/// </summary>
public sealed class FeaturePlanBaseBranchCannotBeRemovedValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        if (payload.EventData is not FeaturePlanBranchRemovedV1 eventData)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(FeaturePlanBaseBranchCannotBeRemovedValidator),
                false,
                $"{nameof(FeaturePlanBaseBranchCannotBeRemovedValidator)} can only validate {nameof(FeaturePlanBranchRemovedV1)} events."
            );
        }

        var isBaseBranch = ((FeatureStateData)stateData).Plans
            .SingleOrDefault(item => item.Id == eventData.PlanId)
            ?.Branch?.Name == eventData.BranchName;

        return EventValidationResult.FromPayload(
            payload,
            nameof(FeaturePlanBaseBranchCannotBeRemovedValidator),
            !isBaseBranch,
            isBaseBranch
                ? "The base branch cannot be removed; update it to another branch instead."
                : null
        );
    }
}
