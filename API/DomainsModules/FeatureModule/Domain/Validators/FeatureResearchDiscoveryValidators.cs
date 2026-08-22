using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

public sealed class FeatureResearchDiscoveryMustNotExistValidator
    : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        if (
            payload.EventData
            is not FeatureResearchDiscoveryAddedV1 eventData
        )
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(FeatureResearchDiscoveryMustNotExistValidator),
                false,
                $"{nameof(FeatureResearchDiscoveryMustNotExistValidator)} can only validate {nameof(FeatureResearchDiscoveryAddedV1)} events."
            );
        }

        var state = (FeatureStateData)stateData;
        var exists = state.ResearchDiscoveries.Any(
            discovery => discovery.Id == eventData.DiscoveryId
        );

        return EventValidationResult.FromPayload(
            payload,
            nameof(FeatureResearchDiscoveryMustNotExistValidator),
            !exists,
            exists ? "The feature research discovery already exists." : null
        );
    }
}

public sealed class FeatureResearchDiscoveryMustExistValidator
    : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var discoveryId = payload.EventData switch
        {
            FeatureResearchDiscoveryUpdatedV1 eventData =>
                eventData.DiscoveryId,
            FeatureResearchDiscoveryRemovedV1 eventData =>
                eventData.DiscoveryId,
            _ => (FeatureResearchDiscoveryId?)null
        };

        if (discoveryId is null)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(FeatureResearchDiscoveryMustExistValidator),
                false,
                $"{nameof(FeatureResearchDiscoveryMustExistValidator)} can only validate discovery update or removal events."
            );
        }

        var state = (FeatureStateData)stateData;
        var exists = state.ResearchDiscoveries.Any(
            discovery => discovery.Id == discoveryId.Value
        );

        return EventValidationResult.FromPayload(
            payload,
            nameof(FeatureResearchDiscoveryMustExistValidator),
            exists,
            exists ? null : "The feature research discovery does not exist."
        );
    }
}
