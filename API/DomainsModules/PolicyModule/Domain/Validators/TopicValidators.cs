using EventSourcing.Shared.Models;
using PolicyModule.Domain.Events;
using PolicyModule.Domain.Models;
using Shared.Interfaces;

namespace PolicyModule.Domain.Validators;

public sealed class TopicMustNotExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var eventData = (TopicCreatedV1)payload.EventData;
        var exists = ((GeneralPoliciesStateData)stateData)
            .Topics
            .ContainsKey(eventData.TopicName);

        return EventValidationResult.FromPayload(
            payload,
            nameof(TopicMustNotExistValidator),
            !exists,
            exists
                ? $"Topic '{eventData.TopicName.Name}' already exists."
                : null
        );
    }
}

public sealed class TopicMustExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var topicName = GetTopicName(payload.EventData);

        var exists = ((GeneralPoliciesStateData)stateData)
            .Topics
            .ContainsKey(topicName);

        return EventValidationResult.FromPayload(
            payload,
            nameof(TopicMustExistValidator),
            exists,
            exists
                ? null
                : $"Topic '{topicName.Name}' does not exist."
        );
    }

    private static TopicName GetTopicName(object eventData) =>
        eventData switch
        {
            TopicUpdatedV1 updated => updated.TopicName,
            TopicRemovedV1 removed => removed.TopicName,
            TopicPolicyAddedV1 added => added.TopicName,
            TopicPolicyUpdatedV1 updated => updated.TopicName,
            TopicPolicyRemovedV1 removed => removed.TopicName,
            _ => throw new InvalidCastException()
        };
}

public sealed class TopicPolicyMustNotExistValidator
    : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var eventData = (TopicPolicyAddedV1)payload.EventData;
        var state = (GeneralPoliciesStateData)stateData;
        var exists = state.Topics.TryGetValue(
            eventData.TopicName,
            out var topic
        ) && topic.Policies.ContainsKey(eventData.Policy.PolicyId);

        return EventValidationResult.FromPayload(
            payload,
            nameof(TopicPolicyMustNotExistValidator),
            !exists,
            exists
                ? $"Policy '{eventData.Policy.PolicyId.Value}' already exists in topic '{eventData.TopicName.Name}'."
                : null
        );
    }
}

public sealed class TopicPolicyMustExistValidator
    : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var eventValues = payload.EventData switch
        {
            TopicPolicyUpdatedV1 updated =>
                (updated.TopicName, updated.Policy.PolicyId),
            TopicPolicyRemovedV1 removed =>
                (removed.TopicName, removed.PolicyId),
            _ => throw new InvalidCastException()
        };

        var state = (GeneralPoliciesStateData)stateData;
        var exists = state.Topics.TryGetValue(
            eventValues.TopicName,
            out var topic
        ) && topic.Policies.ContainsKey(eventValues.PolicyId);

        return EventValidationResult.FromPayload(
            payload,
            nameof(TopicPolicyMustExistValidator),
            exists,
            exists
                ? null
                : $"Policy '{eventValues.PolicyId.Value}' does not exist in topic '{eventValues.TopicName.Name}'."
        );
    }
}
