using EventSourcing.Shared.Models;
using PolicyModule.Domain.Events;
using PolicyModule.Domain.Models;
using Shared.Interfaces;

namespace PolicyModule.Domain.Validators;

public sealed class AgentFamilyMustNotExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var agentFamilyName = payload.EventData switch
        {
            AgentFamilyCreatedV1 data => data.AgentFamilyName,
            AgentFamilyCreatedV2 data => data.AgentFamilyName,
            _ => throw new InvalidCastException()
        };
        var exists = ((GeneralPoliciesStateData)stateData)
            .AgentFamilies
            .ContainsKey(agentFamilyName);

        return EventValidationResult.FromPayload(
            payload,
            nameof(AgentFamilyMustNotExistValidator),
            !exists,
            exists
                ? $"Agent family '{agentFamilyName.Name}' already exists."
                : null
        );
    }
}

public sealed class AgentFamilyMustExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var agentFamilyName = GetAgentFamilyName(payload.EventData);

        var exists = ((GeneralPoliciesStateData)stateData)
            .AgentFamilies
            .ContainsKey(agentFamilyName);

        return EventValidationResult.FromPayload(
            payload,
            nameof(AgentFamilyMustExistValidator),
            exists,
            exists
                ? null
                : $"Agent family '{agentFamilyName.Name}' does not exist."
        );
    }

    private static AgentFamilyName GetAgentFamilyName(object eventData) =>
        eventData switch
        {
            AgentFamilyUpdatedV1 updated => updated.AgentFamilyName,
            AgentFamilyUpdatedV2 updated => updated.AgentFamilyName,
            AgentFamilyRemovedV1 removed => removed.AgentFamilyName,
            AgentFamilyRemovedV2 removed => removed.AgentFamilyName,
            AgentFamilyPolicyAddedV1 added => added.AgentFamilyName,
            AgentFamilyPolicyAddedV2 added => added.AgentFamilyName,
            AgentFamilyPolicyUpdatedV1 updated => updated.AgentFamilyName,
            AgentFamilyPolicyUpdatedV2 updated => updated.AgentFamilyName,
            AgentFamilyPolicyRemovedV1 removed => removed.AgentFamilyName,
            AgentFamilyPolicyRemovedV2 removed => removed.AgentFamilyName,
            _ => throw new InvalidCastException()
        };
}

public sealed class AgentFamilyPolicyMustNotExistValidator
    : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var eventValues = payload.EventData switch
        {
            AgentFamilyPolicyAddedV1 data => (data.AgentFamilyName, data.Policy),
            AgentFamilyPolicyAddedV2 data => (data.AgentFamilyName, data.Policy),
            _ => throw new InvalidCastException()
        };
        var state = (GeneralPoliciesStateData)stateData;
        var exists = state.AgentFamilies.TryGetValue(
            eventValues.AgentFamilyName,
            out var agentFamily
        ) && agentFamily.Policies.ContainsKey(eventValues.Policy.PolicyId);

        return EventValidationResult.FromPayload(
            payload,
            nameof(AgentFamilyPolicyMustNotExistValidator),
            !exists,
            exists
                ? $"Policy '{eventValues.Policy.PolicyId.Value}' already exists in agent family '{eventValues.AgentFamilyName.Name}'."
                : null
        );
    }
}

public sealed class AgentFamilyPolicyMustExistValidator
    : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var eventValues = payload.EventData switch
        {
            AgentFamilyPolicyUpdatedV1 updated =>
                (updated.AgentFamilyName, updated.Policy.PolicyId),
            AgentFamilyPolicyUpdatedV2 updated =>
                (updated.AgentFamilyName, updated.Policy.PolicyId),
            AgentFamilyPolicyRemovedV1 removed =>
                (removed.AgentFamilyName, removed.PolicyId),
            AgentFamilyPolicyRemovedV2 removed =>
                (removed.AgentFamilyName, removed.PolicyId),
            _ => throw new InvalidCastException()
        };

        var state = (GeneralPoliciesStateData)stateData;
        var exists = state.AgentFamilies.TryGetValue(
            eventValues.AgentFamilyName,
            out var agentFamily
        ) && agentFamily.Policies.ContainsKey(eventValues.PolicyId);

        return EventValidationResult.FromPayload(
            payload,
            nameof(AgentFamilyPolicyMustExistValidator),
            exists,
            exists
                ? null
                : $"Policy '{eventValues.PolicyId.Value}' does not exist in agent family '{eventValues.AgentFamilyName.Name}'."
        );
    }
}
