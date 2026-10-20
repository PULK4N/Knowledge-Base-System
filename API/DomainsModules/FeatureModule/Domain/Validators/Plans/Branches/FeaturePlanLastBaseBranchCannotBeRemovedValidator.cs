using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

/// <summary>
/// A plan always keeps at least one base branch. Any base branch can be removed
/// while another one remains; the last one can only be renamed or redescribed.
/// </summary>
public sealed class FeaturePlanLastBaseBranchCannotBeRemovedValidator : IPreEventValidator
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
                nameof(FeaturePlanLastBaseBranchCannotBeRemovedValidator),
                false,
                $"{nameof(FeaturePlanLastBaseBranchCannotBeRemovedValidator)} can only validate {nameof(FeaturePlanBranchRemovedV1)} events."
            );
        }

        var plan = ((FeatureStateData)stateData).Plans
            .SingleOrDefault(item => item.Id == eventData.PlanId);
        var isLastBaseBranch = plan?.Branches is [var onlyBranch]
            && onlyBranch.Name == eventData.BranchName;

        return EventValidationResult.FromPayload(
            payload,
            nameof(FeaturePlanLastBaseBranchCannotBeRemovedValidator),
            !isLastBaseBranch,
            isLastBaseBranch
                ? "The last base branch cannot be removed; update it to another branch instead."
                : null
        );
    }
}
