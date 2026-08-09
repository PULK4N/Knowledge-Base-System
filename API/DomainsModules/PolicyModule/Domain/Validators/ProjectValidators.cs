using EventSourcing.Shared.Models;
using PolicyModule.Domain.Events;
using Shared.Interfaces;

namespace PolicyModule.Domain.Validators;

public sealed class ProjectMustNotExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var exists = !string.IsNullOrWhiteSpace(
            ((ProjectPoliciesStateData)stateData).ProjectName
        );

        return EventValidationResult.FromPayload(
            payload,
            nameof(ProjectMustNotExistValidator),
            !exists,
            exists ? "The project already exists." : null
        );
    }
}

public sealed class ProjectMustBeActiveValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var state = (ProjectPoliciesStateData)stateData;
        var exists = !string.IsNullOrWhiteSpace(state.ProjectName);
        var active = exists && !state.IsDeleted;
        var failureReason = !exists
            ? "The project does not exist."
            : state.IsDeleted
                ? "The project is deleted."
                : null;

        return EventValidationResult.FromPayload(
            payload,
            nameof(ProjectMustBeActiveValidator),
            active,
            failureReason
        );
    }
}

public sealed class ProjectPolicyMustNotExistValidator
    : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var eventData = (ProjectPolicyAddedV1)payload.EventData;
        var exists = ((ProjectPoliciesStateData)stateData)
            .Policies
            .ContainsKey(eventData.Policy.PolicyId);

        return EventValidationResult.FromPayload(
            payload,
            nameof(ProjectPolicyMustNotExistValidator),
            !exists,
            exists
                ? $"Project policy '{eventData.Policy.PolicyId.Value}' already exists."
                : null
        );
    }
}

public sealed class ProjectRepositoryMustNotExistValidator
    : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var eventData = (RepositoryAddedToProjectV1)payload.EventData;
        var exists = ((ProjectPoliciesStateData)stateData)
            .RepositoryPaths
            .Contains(eventData.RepositoryPath, StringComparer.Ordinal);

        return EventValidationResult.FromPayload(
            payload,
            nameof(ProjectRepositoryMustNotExistValidator),
            !exists,
            exists
                ? $"Repository path '{eventData.RepositoryPath}' is already assigned to the project."
                : null
        );
    }
}

public sealed class ProjectPolicyMustExistValidator
    : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var policyId = payload.EventData switch
        {
            ProjectPolicyUpdatedV1 updated => updated.Policy.PolicyId,
            ProjectPolicyRemovedV1 removed => removed.PolicyId,
            _ => throw new InvalidCastException()
        };

        var exists = ((ProjectPoliciesStateData)stateData)
            .Policies
            .ContainsKey(policyId);

        return EventValidationResult.FromPayload(
            payload,
            nameof(ProjectPolicyMustExistValidator),
            exists,
            exists
                ? null
                : $"Project policy '{policyId.Value}' does not exist."
        );
    }
}

public sealed class ProjectTopicMustNotExistValidator
    : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var eventData =
            (TopicRelationAddedToProjectV1)payload.EventData;
        var exists = ((ProjectPoliciesStateData)stateData)
            .RelatedTopics
            .Contains(eventData.TopicName);

        return EventValidationResult.FromPayload(
            payload,
            nameof(ProjectTopicMustNotExistValidator),
            !exists,
            exists
                ? $"Topic '{eventData.TopicName.Name}' is already related to the project."
                : null
        );
    }
}

public sealed class ProjectTopicMustExistValidator
    : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var eventData =
            (TopicRelationRemovedFromProjectV1)payload.EventData;
        var exists = ((ProjectPoliciesStateData)stateData)
            .RelatedTopics
            .Contains(eventData.TopicName);

        return EventValidationResult.FromPayload(
            payload,
            nameof(ProjectTopicMustExistValidator),
            exists,
            exists
                ? null
                : $"Topic '{eventData.TopicName.Name}' is not related to the project."
        );
    }
}
