using ProjectModule.Shared;
using EventSourcing.Shared.Models;
using ProjectModule.Domain.Events;
using Shared.Interfaces;

namespace ProjectModule.Domain.Validators;

/// <summary>Every mapped review level names both a model and a reasoning setting.</summary>
public sealed class ProjectReviewLevelModelsMustBeCompleteValidator
    : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var levelModels = payload.EventData switch
        {
            ProjectReviewLevelModelsSetV1 data => data.LevelModels,
            _ => throw new InvalidCastException()
        };
        var incompleteLevels = levelModels
            .Where(
                levelModel => string.IsNullOrWhiteSpace(levelModel.Value.Model)
                    || string.IsNullOrWhiteSpace(levelModel.Value.Reasoning)
            )
            .Select(levelModel => levelModel.Key.ToString())
            .Order(StringComparer.Ordinal)
            .ToList();

        return EventValidationResult.FromPayload(
            payload,
            nameof(ProjectReviewLevelModelsMustBeCompleteValidator),
            incompleteLevels.Count == 0,
            incompleteLevels.Count == 0
                ? null
                : $"Review levels need a model and a reasoning: {string.Join(", ", incompleteLevels)}."
        );
    }
}
