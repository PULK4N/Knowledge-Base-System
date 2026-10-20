using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

/// <summary>
/// A plan may link only records of its own feature, each once.
/// </summary>
public sealed class FeaturePlanRecordsMustExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        if (payload.EventData is not FeaturePlanRecordsSetV1 eventData)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(FeaturePlanRecordsMustExistValidator),
                false,
                $"{nameof(FeaturePlanRecordsMustExistValidator)} can only validate {nameof(FeaturePlanRecordsSetV1)} events."
            );
        }

        var state = (FeatureStateData)stateData;
        var recordIds = state.Records
            .Select(record => record.Id)
            .ToHashSet();
        var error =
            eventData.RecordIds.Distinct().Count() != eventData.RecordIds.Length
                ? "A plan cannot link the same record more than once."
                : eventData.RecordIds.Any(recordId => !recordIds.Contains(recordId))
                    ? "A linked feature record does not exist."
                    : null;

        return EventValidationResult.FromPayload(
            payload,
            nameof(FeaturePlanRecordsMustExistValidator),
            error is null,
            error
        );
    }
}
