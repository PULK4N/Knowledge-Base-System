using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using ProjectModule.Shared;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

public sealed class FeatureReviewerContentValidator : IPreEventValidator
{
    public EventValidationResult Validate(object stateData, EventPayload payload)
    {
        var valid = payload.EventData switch
        {
            FeatureReviewerAddedV1 data => Valid(data.Title, data.Description, data.ReviewLevel),
            FeatureReviewerUpdatedV1 data => Valid(data.Title, data.Description, data.ReviewLevel),
            _ => false
        };
        return EventValidationResult.FromPayload(payload, nameof(FeatureReviewerContentValidator), valid,
            valid ? null : "A title, description and valid review level are required.");
    }

    private static bool Valid(string title, string description, ReviewLevel level) =>
        !string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(description) && Enum.IsDefined(level);
}
