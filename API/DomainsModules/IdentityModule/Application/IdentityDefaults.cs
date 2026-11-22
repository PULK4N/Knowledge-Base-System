using EventSourcing.Shared.Models;
using IdentityModule.Domain;
using Microsoft.AspNetCore.Identity;
using SharedModule.Constants;

namespace IdentityModule.Application;

public static class IdentityDefaults
{
    public const string StateMachineId = "identity-state-machine";

    /// <summary>The administrator's fixed username, seeded by migration together with the account.</summary>
    public const string AdministratorUserName = "admin";

    /// <summary>Accepted until the administrator sets their own password.</summary>
    public const string InitialPassword = "123";

    public static AggregateId AdministratorId =>
        AggregateId.FromDatabaseGuid(StateDataAggregateIds.AdministratorAccount);

    public static bool UserNamesMatch(string first, string second) =>
        string.Equals(first.Trim(), second.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>The seeded administrator has no password hash, so only the initial password matches it.</summary>
    public static bool MustChangePassword(AccountStateData administrator) =>
        administrator.PasswordHash.Length == 0;

    public static bool PasswordMatches(IPasswordHasher<AccountStateData> hasher, AccountStateData administrator,
        string password) =>
        MustChangePassword(administrator)
            ? password == InitialPassword
            : hasher.VerifyHashedPassword(administrator, administrator.PasswordHash, password)
              != PasswordVerificationResult.Failed;

    public static bool IsValidNewPassword(string newPassword, string confirmPassword) =>
        !string.IsNullOrWhiteSpace(newPassword)
        && newPassword == confirmPassword
        && newPassword != InitialPassword;
}
