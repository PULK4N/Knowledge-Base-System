using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

public sealed class FeatureReviewerMustExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(object stateData, EventPayload payload)
    {
        var reviewerId = payload.EventData switch
        {
            FeatureReviewerUpdatedV1 data => data.ReviewerId,
            FeatureReviewerRemovedV1 data => data.ReviewerId,
            _ => (FeatureReviewerId?)null
        };
        var exists = ((FeatureStateData)stateData).Reviewers.Any(item => item.Id == reviewerId);
        var valid = reviewerId is not null && exists;
        return EventValidationResult.FromPayload(payload, nameof(FeatureReviewerMustExistValidator), valid,
            valid ? null : "The feature reviewer does not exist.");
    }
}
