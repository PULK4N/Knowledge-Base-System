using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

public sealed class FeatureReviewNoteMustNotExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        if (payload.EventData is not FeatureReviewNoteAddedV1 eventData)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(FeatureReviewNoteMustNotExistValidator),
                false,
                $"{nameof(FeatureReviewNoteMustNotExistValidator)} can only validate {nameof(FeatureReviewNoteAddedV1)} events."
            );
        }

        var state = (FeatureStateData)stateData;
        var exists = state.ReviewNotes.Any(
            reviewNote => reviewNote.Id == eventData.ReviewNoteId
        );

        return EventValidationResult.FromPayload(
            payload,
            nameof(FeatureReviewNoteMustNotExistValidator),
            !exists,
            exists ? "The feature review note already exists." : null
        );
    }
}

public sealed class FeatureReviewNoteMustExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var reviewNoteId = payload.EventData switch
        {
            FeatureReviewNoteUpdatedV1 eventData => eventData.ReviewNoteId,
            FeatureReviewNoteRemovedV1 eventData => eventData.ReviewNoteId,
            _ => (FeatureReviewNoteId?)null
        };

        if (reviewNoteId is null)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(FeatureReviewNoteMustExistValidator),
                false,
                $"{nameof(FeatureReviewNoteMustExistValidator)} can only validate review note update or removal events."
            );
        }

        var state = (FeatureStateData)stateData;
        var exists = state.ReviewNotes.Any(
            reviewNote => reviewNote.Id == reviewNoteId.Value
        );

        return EventValidationResult.FromPayload(
            payload,
            nameof(FeatureReviewNoteMustExistValidator),
            exists,
            exists ? null : "The feature review note does not exist."
        );
    }
}
