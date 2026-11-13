using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

/// <summary>
/// A plan can be completed only when every definition of done is Confirmed.
/// Unconfirmed ones must be confirmed or moved to another plan first.
/// </summary>
public sealed class FeaturePlanDefinitionsOfDoneMustBeConfirmedValidator : IPreEventValidator
{
    public EventValidationResult Validate(object stateData, EventPayload payload)
    {
        var (planId, status) = payload.EventData switch
        {
            FeaturePlanStatusChangedV1 data => (data.PlanId, data.Status),
            FeaturePlanUpdatedV1 data => (data.PlanId, data.Status),
            _ => (default(FeaturePlanId?), default(FeaturePlanStatus?))
        };
        if (planId is null)
            return Result(payload, $"{nameof(FeaturePlanDefinitionsOfDoneMustBeConfirmedValidator)} cannot validate {payload.EventData.GetType().Name}.");
        if (status != FeaturePlanStatus.Completed)
            return Result(payload, null);

        var blocking = ((FeatureStateData)stateData).Plans
            .SingleOrDefault(plan => plan.Id == planId)?.Specification.DefinitionsOfDone
            .Where(item => item.State != DefinitionOfDoneState.Confirmed)
            .OrderBy(item => item.Number)
            .Select(item => $"DOD-{item.Number} ({item.State})")
            .ToList() ?? [];
        return Result(payload, blocking.Count == 0
            ? null
            : "The plan cannot be completed until every definition of done is Confirmed. Confirm or move to another plan: "
              + string.Join(", ", blocking) + ".");
    }

    private static EventValidationResult Result(EventPayload payload, string? error) =>
        EventValidationResult.FromPayload(payload, nameof(FeaturePlanDefinitionsOfDoneMustBeConfirmedValidator), error is null, error);
}
