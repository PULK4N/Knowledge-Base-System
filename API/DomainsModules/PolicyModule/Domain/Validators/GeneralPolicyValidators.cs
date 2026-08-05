using EventSourcing.Shared.Models;
using PolicyModule.Domain.Events;
using Shared.Interfaces;

namespace PolicyModule.Domain.Validators;

public sealed class GeneralPolicyMustNotExistValidator
    : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var eventData = (GeneralPolicyAddedV1)payload.EventData;
        var state = (GeneralPoliciesStateData)stateData;
        var exists = state.Policies.ContainsKey(
            eventData.Policy.PolicyId
        );

        return EventValidationResult.FromPayload(
            payload,
            nameof(GeneralPolicyMustNotExistValidator),
            !exists,
            exists
                ? $"A general policy with ID '{eventData.Policy.PolicyId.Value}' already exists."
                : null
        );
    }
}

public sealed class GeneralPolicyMustExistValidator
    : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var policyId = payload.EventData switch
        {
            GeneralPolicyUpdatedV1 eventData =>
                eventData.Policy.PolicyId,
            GeneralPolicyRemovedV1 eventData => eventData.PolicyId,
            _ => throw new InvalidCastException()
        };

        var exists = ((GeneralPoliciesStateData)stateData)
            .Policies
            .ContainsKey(policyId);

        return EventValidationResult.FromPayload(
            payload,
            nameof(GeneralPolicyMustExistValidator),
            exists,
            exists
                ? null
                : $"A general policy with ID '{policyId.Value}' does not exist."
        );
    }
}
