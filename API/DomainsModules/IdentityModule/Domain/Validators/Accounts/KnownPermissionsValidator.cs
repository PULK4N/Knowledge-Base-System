using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;
using IdentityModule.Domain.Events;
using IdentityModule.Shared;
using Shared.Interfaces;

namespace IdentityModule.Domain.Validators;

public sealed class KnownPermissionsValidator : IPreEventValidator
{
    public EventValidationResult Validate(object stateData, EventPayload payload)
    {
        var permissions = payload.EventData switch
        {
            AgentCreatedV1 value => value.Permissions,
            AgentPermissionsSetV1 value => value.Permissions,
            _ => throw new InvalidCastException()
        };
        var unknown = permissions.Where(permission => !Permissions.All.Contains(new Permission(permission))).ToList();
        return EventValidationResult.FromPayload(payload, nameof(KnownPermissionsValidator), unknown.Count == 0,
            unknown.Count == 0 ? null : $"Unknown permissions: {string.Join(", ", unknown)}.");
    }
}
