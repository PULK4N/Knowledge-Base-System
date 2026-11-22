using EventSourcing.Shared.Models;

namespace IdentityModule.Domain.Constraints;

public sealed class UniqueAccountUserNameConstraint(AccountStateData stateData, string normalizedUserName)
    : UniqueConstraint(stateData, normalizedUserName);
