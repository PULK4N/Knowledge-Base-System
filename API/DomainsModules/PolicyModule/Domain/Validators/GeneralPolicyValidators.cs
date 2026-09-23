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
        var policy = payload.EventData switch
        {
            GeneralPolicyAddedV1 data => data.Policy,
            GeneralPolicyAddedV2 data => data.Policy,
            _ => throw new InvalidCastException()
        };
        var state = (GeneralPoliciesStateData)stateData;
        var exists = state.Policies.ContainsKey(
            policy.PolicyId
        );

        return EventValidationResult.FromPayload(
            payload,
            nameof(GeneralPolicyMustNotExistValidator),
            !exists,
            exists
                ? $"A general policy with ID '{policy.PolicyId.Value}' already exists."
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
            GeneralPolicyUpdatedV2 eventData =>
                eventData.Policy.PolicyId,
            GeneralPolicyRemovedV1 eventData => eventData.PolicyId,
            GeneralPolicyRemovedV2 eventData => eventData.PolicyId,
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
