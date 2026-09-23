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
        var policy = payload.EventData switch
        {
            ProjectPolicyAddedV1 data => data.Policy,
            ProjectPolicyAddedV2 data => data.Policy,
            _ => throw new InvalidCastException()
        };
        var exists = ((ProjectPoliciesStateData)stateData)
            .Policies
            .ContainsKey(policy.PolicyId);

        return EventValidationResult.FromPayload(
            payload,
            nameof(ProjectPolicyMustNotExistValidator),
            !exists,
            exists
                ? $"Project policy '{policy.PolicyId.Value}' already exists."
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
        var repositoryPath = payload.EventData switch
        {
            RepositoryAddedToProjectV1 data => data.RepositoryPath,
            RepositoryAddedToProjectV2 data => data.RepositoryPath,
            _ => throw new InvalidCastException()
        };
        var exists = ((ProjectPoliciesStateData)stateData)
            .RepositoryPaths
            .Contains(repositoryPath, StringComparer.Ordinal);

        return EventValidationResult.FromPayload(
            payload,
            nameof(ProjectRepositoryMustNotExistValidator),
            !exists,
            exists
                ? $"Repository path '{repositoryPath}' is already assigned to the project."
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
            ProjectPolicyUpdatedV2 updated => updated.Policy.PolicyId,
            ProjectPolicyRemovedV1 removed => removed.PolicyId,
            ProjectPolicyRemovedV2 removed => removed.PolicyId,
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
        var topicName = payload.EventData switch
        {
            TopicRelationAddedToProjectV1 data => data.TopicName,
            TopicRelationAddedToProjectV2 data => data.TopicName,
            _ => throw new InvalidCastException()
        };
        var exists = ((ProjectPoliciesStateData)stateData)
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
        var exists = ((ProjectPoliciesStateData)stateData)
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
