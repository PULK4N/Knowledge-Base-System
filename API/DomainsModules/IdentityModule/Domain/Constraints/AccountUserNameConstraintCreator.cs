using EventSourcing.Shared.Interfaces;

namespace IdentityModule.Domain.Constraints;

/// <summary>Keeps usernames unique across the administrator and every agent, ignoring case.</summary>
public sealed class AccountUserNameConstraintCreator
    : IUniqueConstraintCreator<AccountStateData, UniqueAccountUserNameConstraint>
{
    public List<UniqueAccountUserNameConstraint> Create(AccountStateData stateData)
    {
        var normalizedUserName = stateData.UserName.Trim().ToUpperInvariant();

        return normalizedUserName.Length == 0
            ? []
            : [new UniqueAccountUserNameConstraint(stateData, normalizedUserName)];
    }
}
