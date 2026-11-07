using EventSourcing.Shared.Models;
using ProjectModule.Domain.Events;
using Shared.Interfaces;

namespace ProjectModule.Domain.Validators;

public sealed class ProjectTopicMustNotExistValidator
    : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var topicName = payload.EventData switch
        {
            TopicRelationAddedToProjectV1 data => data.TopicName,
            TopicRelationAddedToProjectV2 data => data.TopicName,
            _ => throw new InvalidCastException()
        };
        var exists = ((ProjectStateData)stateData)
            .RelatedTopics
            .Contains(topicName);

        return EventValidationResult.FromPayload(
            payload,
            nameof(ProjectTopicMustNotExistValidator),
            !exists,
            exists
                ? $"Topic '{topicName.Name}' is already related to the project."
                : null
        );
    }
}
