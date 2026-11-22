using EventSourcing.Shared.Models;
using IdentityModule.Domain.Events;
using Shared.Interfaces;

namespace IdentityModule.Domain.Validators;

public sealed class UserNameRequiredValidator : IPreEventValidator
{
    public EventValidationResult Validate(object stateData, EventPayload payload)
    {
        var userName = payload.EventData switch
        {
            AdministratorCreatedV1 value => value.UserName,
            AgentCreatedV1 value => value.UserName,
            _ => throw new InvalidCastException()
        };
        var valid = !string.IsNullOrWhiteSpace(userName);
        return EventValidationResult.FromPayload(payload, nameof(UserNameRequiredValidator), valid,
            valid ? null : "Username is required.");
    }
}
