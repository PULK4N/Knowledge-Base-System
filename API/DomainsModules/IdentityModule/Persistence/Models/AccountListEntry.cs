namespace IdentityModule.Persistence.Models;

/// <summary>One account without its password hash, for listing agents.</summary>
public sealed class AccountListEntry
{
    public required Guid AccountId { get; set; }
    public required string UserName { get; set; }
    public required string DisplayName { get; set; }
    public bool IsAdministrator { get; set; }
    public bool IsAgent { get; set; }
    public List<string> Permissions { get; set; } = [];
    public DateTime UpdatedAt { get; set; }
    public long ProjectedOrderNumber { get; set; }
}
