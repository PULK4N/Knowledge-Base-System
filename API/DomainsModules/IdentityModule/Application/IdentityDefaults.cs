using EventSourcing.Shared.Models;
using SharedModule.Constants;

namespace IdentityModule.Application;

public static class IdentityDefaults
{
    public const string StateMachineId = "identity-state-machine";

    /// <summary>Accepted until the administrator sets their own credentials on first login.</summary>
    public const string InitialUserName = "admin";

    /// <inheritdoc cref="InitialUserName"/>
    public const string InitialPassword = "123";

    public static AggregateId AdministratorId =>
        AggregateId.FromDatabaseGuid(StateDataAggregateIds.AdministratorAccount);

    public static bool UserNamesMatch(string first, string second) =>
        string.Equals(first.Trim(), second.Trim(), StringComparison.OrdinalIgnoreCase);

    public static bool IsValidNewPassword(string newPassword, string confirmPassword) =>
        !string.IsNullOrWhiteSpace(newPassword)
        && newPassword == confirmPassword
        && newPassword != InitialPassword;
}
