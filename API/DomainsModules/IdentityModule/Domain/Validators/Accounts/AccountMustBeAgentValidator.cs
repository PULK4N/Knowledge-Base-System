using EventSourcing.Shared.Models;
using Shared.Interfaces;

namespace IdentityModule.Domain.Validators;

public sealed class AccountMustBeAgentValidator : IPreEventValidator
{
    public EventValidationResult Validate(object stateData, EventPayload payload)
    {
        var valid = ((AccountStateData)stateData).IsAgent;
        return EventValidationResult.FromPayload(payload, nameof(AccountMustBeAgentValidator), valid,
            valid ? null : "The account is not an agent.");
    }
}
