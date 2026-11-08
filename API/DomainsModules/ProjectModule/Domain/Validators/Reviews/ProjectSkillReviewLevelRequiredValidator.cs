using EventSourcing.Shared.Models;
using ProjectModule.Domain.Events;
using Shared.Interfaces;

namespace ProjectModule.Domain.Validators;

/// <summary>A skill used for review must say how demanding its review is.</summary>
public sealed class ProjectSkillReviewLevelRequiredValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var (usedForReview, reviewLevel) = payload.EventData switch
        {
            ProjectSkillRelationAddedV2 data => (data.UsedForReview, data.ReviewLevel),
            ProjectSkillRelationUpdatedV1 data => (data.UsedForReview, data.ReviewLevel),
            _ => throw new InvalidCastException()
        };
        var valid = !usedForReview || reviewLevel is not null;

        return EventValidationResult.FromPayload(
            payload,
            nameof(ProjectSkillReviewLevelRequiredValidator),
            valid,
            valid ? null : "A skill used for review needs a review level."
        );
    }
}
