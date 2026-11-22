using EventSourcing.Shared.Models;
using Shared.Interfaces;

namespace IdentityModule.Domain.Validators;

public sealed class AccountMustNotExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(object stateData, EventPayload payload)
    {
        var valid = !((AccountStateData)stateData).IsCreated;
        return EventValidationResult.FromPayload(payload, nameof(AccountMustNotExistValidator), valid,
            valid ? null : "The account already exists.");
    }
}
