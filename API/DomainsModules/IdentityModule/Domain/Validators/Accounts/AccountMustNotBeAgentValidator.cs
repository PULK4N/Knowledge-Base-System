using EventSourcing.Shared.Models;
using Shared.Interfaces;

namespace IdentityModule.Domain.Validators;

/// <summary>Only the administrator account has a password; agents use session keys.</summary>
public sealed class AccountMustNotBeAgentValidator : IPreEventValidator
{
    public EventValidationResult Validate(object stateData, EventPayload payload)
    {
        var valid = !((AccountStateData)stateData).IsAgent;
        return EventValidationResult.FromPayload(payload, nameof(AccountMustNotBeAgentValidator), valid,
            valid ? null : "Agent accounts have no password.");
    }
}
