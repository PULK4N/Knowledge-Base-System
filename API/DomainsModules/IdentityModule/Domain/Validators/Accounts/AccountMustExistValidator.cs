using EventSourcing.Shared.Models;
using Shared.Interfaces;

namespace IdentityModule.Domain.Validators;

public sealed class AccountMustExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(object stateData, EventPayload payload)
    {
        var valid = ((AccountStateData)stateData).IsCreated;
        return EventValidationResult.FromPayload(payload, nameof(AccountMustExistValidator), valid,
            valid ? null : "The account does not exist.");
    }
}
