using EventSourcing.Shared.Models;
using ProjectModule.Domain.Events;
using Shared.Interfaces;

namespace ProjectModule.Domain.Validators;

public sealed class ProjectTopicMustExistValidator
    : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var topicName = payload.EventData switch
        {
            TopicRelationRemovedFromProjectV1 data => data.TopicName,
            TopicRelationRemovedFromProjectV2 data => data.TopicName,
            _ => throw new InvalidCastException()
        };
        var exists = ((ProjectStateData)stateData)
            .RelatedTopics
            .Contains(topicName);

        return EventValidationResult.FromPayload(
            payload,
            nameof(ProjectTopicMustExistValidator),
            exists,
            exists
                ? null
                : $"Topic '{topicName.Name}' is not related to the project."
        );
    }
}
