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
        var topicName = payload.EventData switch
        {
            TopicCreatedV1 data => data.TopicName,
            TopicCreatedV2 data => data.TopicName,
            _ => throw new InvalidCastException()
        };
        var exists = ((GeneralPoliciesStateData)stateData)
            .Topics
            .ContainsKey(topicName);

        return EventValidationResult.FromPayload(
            payload,
            nameof(TopicMustNotExistValidator),
            !exists,
            exists
                ? $"Topic '{topicName.Name}' already exists."
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
            TopicUpdatedV2 updated => updated.TopicName,
            TopicRemovedV1 removed => removed.TopicName,
            TopicRemovedV2 removed => removed.TopicName,
            TopicPolicyAddedV1 added => added.TopicName,
            TopicPolicyAddedV2 added => added.TopicName,
            TopicPolicyUpdatedV1 updated => updated.TopicName,
            TopicPolicyUpdatedV2 updated => updated.TopicName,
            TopicPolicyRemovedV1 removed => removed.TopicName,
            TopicPolicyRemovedV2 removed => removed.TopicName,
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
        var eventValues = payload.EventData switch
        {
            TopicPolicyAddedV1 data => (data.TopicName, data.Policy),
            TopicPolicyAddedV2 data => (data.TopicName, data.Policy),
            _ => throw new InvalidCastException()
        };
        var state = (GeneralPoliciesStateData)stateData;
        var exists = state.Topics.TryGetValue(
            eventValues.TopicName,
            out var topic
        ) && topic.Policies.ContainsKey(eventValues.Policy.PolicyId);

        return EventValidationResult.FromPayload(
            payload,
            nameof(TopicPolicyMustNotExistValidator),
            !exists,
            exists
                ? $"Policy '{eventValues.Policy.PolicyId.Value}' already exists in topic '{eventValues.TopicName.Name}'."
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
            TopicPolicyUpdatedV2 updated =>
                (updated.TopicName, updated.Policy.PolicyId),
            TopicPolicyRemovedV1 removed =>
                (removed.TopicName, removed.PolicyId),
            TopicPolicyRemovedV2 removed =>
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
