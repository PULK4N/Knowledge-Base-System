using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

/// <summary>
/// A previous plan must be another plan of the same feature, and following it
/// must not lead back to the plan itself or make branches come from themselves.
/// </summary>
public sealed class FeaturePreviousPlanValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        (FeaturePlanId PlanId, FeaturePlanId? PreviousPlanId)? link = payload.EventData switch
        {
            FeaturePlanAddedV4 eventData => (eventData.PlanId, eventData.PreviousPlanId),
            FeaturePlanPreviousPlanSetV1 eventData => (eventData.PlanId, eventData.PreviousPlanId),
            _ => null
        };

        if (link is null)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(FeaturePreviousPlanValidator),
                false,
                $"{nameof(FeaturePreviousPlanValidator)} can only validate plan addition or previous-plan events."
            );
        }

        var (planId, previousPlanId) = link.Value;
        var plans = ((FeatureStateData)stateData).Plans
            .ToDictionary(plan => plan.Id);
        var error = previousPlanId switch
        {
            null => null,
            { } id when !plans.ContainsKey(id) => "The previous plan does not exist in this feature.",
            { } id when LeadsBackTo(planId, id, plans) => "A plan cannot follow itself or a plan that comes after it.",
            _ when MakesBranchCycle(planId, previousPlanId, plans.Values) =>
                "The plan's base branch would come from a branch created from it.",
            _ => null
        };

        return EventValidationResult.FromPayload(
            payload,
            nameof(FeaturePreviousPlanValidator),
            error is null,
            error
        );
    }

    /// <summary>
    /// A base branch without sources comes from the previous plan, so a new
    /// previous plan must not make branches come from themselves.
    /// </summary>
    private static bool MakesBranchCycle(
        FeaturePlanId planId,
        FeaturePlanId? previousPlanId,
        IReadOnlyCollection<FeaturePlan> plans
    ) =>
        FeatureBranchGraph.HasCycle(
            FeatureBranchGraph.Build(
                plans,
                previousPlanOf: plan => plan.Id == planId
                    ? previousPlanId
                    : plan.PreviousPlanId
            )
        );

    private static bool LeadsBackTo(
        FeaturePlanId planId,
        FeaturePlanId previousPlanId,
        Dictionary<FeaturePlanId, FeaturePlan> plans
    )
    {
        var visited = new HashSet<FeaturePlanId>();
        FeaturePlanId? current = previousPlanId;

        while (current is { } id && visited.Add(id))
        {
            if (id == planId)
                return true;

            current = plans.TryGetValue(id, out var plan) ? plan.PreviousPlanId : null;
        }

        return false;
    }
}
