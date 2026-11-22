using IdentityModule.Domain;

namespace IdentityModule.Application.DTOs;

public sealed record AgentDto(
    Guid Id,
    string UserName,
    string DisplayName,
    bool IsAdministrator,
    List<string> Permissions)
{
    public static AgentDto FromStateData(AccountStateData account) =>
        new(account.Id.Value, account.UserName, account.DisplayName, account.IsAdministrator,
            account.Permissions.ToList());
}
