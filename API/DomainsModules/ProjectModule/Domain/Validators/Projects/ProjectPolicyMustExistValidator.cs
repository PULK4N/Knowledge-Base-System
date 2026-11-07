using EventSourcing.Shared.Models;
using ProjectModule.Domain.Events;
using Shared.Interfaces;

namespace ProjectModule.Domain.Validators;

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

        var exists = ((ProjectStateData)stateData)
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
