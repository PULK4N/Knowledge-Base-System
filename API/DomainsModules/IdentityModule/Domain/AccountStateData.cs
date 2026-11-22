using EventSourcing.Shared.Models;

namespace IdentityModule.Domain;

/// <summary>
/// One account: the single administrator, or an agent. Only the administrator has a password hash;
/// agents authenticate with session keys.
/// </summary>
public sealed class AccountStateData(AggregateId id) : ISharedStateData
{
    public AggregateId Id { get; init; } = id;
    public bool IsDeleted { get; set; }
    public bool IsCreated { get; set; }
    public bool IsAgent { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsAdministrator { get; set; }
    public List<string> Permissions { get; set; } = [];
}
