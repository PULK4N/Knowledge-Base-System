using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

// Research discoveries replay into records; these validators stay registered
// for the released research events and check the record with the same ID.

public sealed class FeatureResearchDiscoveryMustNotExistValidator
    : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var discoveryId = payload.EventData switch
        {
            FeatureResearchDiscoveryAddedV1 eventData =>
                eventData.DiscoveryId,
            FeatureResearchDiscoveryAddedV2 eventData =>
                eventData.DiscoveryId,
            FeatureResearchDiscoveryAddedV3 eventData =>
                eventData.DiscoveryId,
            _ => (FeatureResearchDiscoveryId?)null
        };

        if (discoveryId is null)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(FeatureResearchDiscoveryMustNotExistValidator),
                false,
                $"{nameof(FeatureResearchDiscoveryMustNotExistValidator)} can only validate discovery add events."
            );
        }

        var state = (FeatureStateData)stateData;
        var recordId = discoveryId.Value.ToRecordId();
        var exists = state.Records.Any(record => record.Id == recordId);

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
            FeatureResearchDiscoveryUpdatedV2 eventData =>
                eventData.DiscoveryId,
            FeatureResearchDiscoveryUpdatedV3 eventData =>
                eventData.DiscoveryId,
            FeatureResearchDiscoveryRemovedV1 eventData =>
                eventData.DiscoveryId,
            FeatureResearchDiscoveryRemovedV2 eventData =>
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
        var recordId = discoveryId.Value.ToRecordId();
        var exists = state.Records.Any(record => record.Id == recordId);

        return EventValidationResult.FromPayload(
            payload,
            nameof(FeatureResearchDiscoveryMustExistValidator),
            exists,
            exists ? null : "The feature research discovery does not exist."
        );
    }
}
