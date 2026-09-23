using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

public sealed class FeatureRecordMustNotExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var recordId = payload.EventData switch
        {
            FeatureRecordAddedV1 eventData => eventData.RecordId,
            FeatureRecordAddedV2 eventData => eventData.RecordId,
            _ => (FeatureRecordId?)null
        };

        if (recordId is null)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(FeatureRecordMustNotExistValidator),
                false,
                $"{nameof(FeatureRecordMustNotExistValidator)} can only validate {nameof(FeatureRecordAddedV1)} or {nameof(FeatureRecordAddedV2)} events."
            );
        }

        return ValidateAbsence(
            (FeatureStateData)stateData,
            payload,
            recordId.Value
        );
    }

    private static EventValidationResult ValidateAbsence(
        FeatureStateData state,
        EventPayload payload,
        FeatureRecordId recordId
    )
    {
        var exists = state.Records.Any(record => record.Id == recordId);

        return EventValidationResult.FromPayload(
            payload,
            nameof(FeatureRecordMustNotExistValidator),
            !exists,
            exists ? "The feature record already exists." : null
        );
    }
}

public sealed class FeatureRecordMustExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var recordId = payload.EventData switch
        {
            FeatureRecordUpdatedV1 eventData => eventData.RecordId,
            FeatureRecordUpdatedV2 eventData => eventData.RecordId,
            FeatureRecordRemovedV1 eventData => eventData.RecordId,
            FeatureRecordRemovedV2 eventData => eventData.RecordId,
            _ => (FeatureRecordId?)null
        };

        if (recordId is null)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(FeatureRecordMustExistValidator),
                false,
                $"{nameof(FeatureRecordMustExistValidator)} can only validate record update or removal events."
            );
        }

        var state = (FeatureStateData)stateData;
        var exists = state.Records.Any(record => record.Id == recordId.Value);

        return EventValidationResult.FromPayload(
            payload,
            nameof(FeatureRecordMustExistValidator),
            exists,
            exists ? null : "The feature record does not exist."
        );
    }
}
