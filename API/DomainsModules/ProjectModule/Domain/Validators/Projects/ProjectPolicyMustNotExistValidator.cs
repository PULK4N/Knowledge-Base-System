using EventSourcing.Shared.Models;
using ProjectModule.Domain.Events;
using Shared.Interfaces;

namespace ProjectModule.Domain.Validators;

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
        var exists = ((ProjectStateData)stateData)
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
