using IdentityModule.Application.Commands;
using IdentityModule.Domain;
using IdentityModule.Domain.Events;
using Microsoft.AspNetCore.Identity;

namespace IdentityModule.Application.Tests;

public sealed class AdministratorCommandTests
{
    [Theory]
    [InlineData(null, "123")]
    [InlineData("old-secret", "old-secret")]
    public async Task ChangePassword_WritesNewHashForTheAdministrator(string? storedPassword, string currentPassword)
    {
        var context = new IdentityTestContext().WithAdministrator(storedPassword);

        Assert.True(await ChangePassword(context, currentPassword, "new-secret", "new-secret")
            .Execute(IdentityTestContext.Executor));

        var payload = Assert.Single(context.Written);
        Assert.Equal(IdentityDefaults.AdministratorId, payload.EventExecutionInfo.AggregateId);
        Assert.Equal(IdentityTestContext.Executor.Id, payload.EventExecutionInfo.EventExecutor);
        var changed = Assert.IsType<AdministratorPasswordChangedV1>(payload.EventData);
        Assert.DoesNotContain("new-secret", changed.PasswordHash);
        AssertPasswordMatches(changed.PasswordHash, "new-secret");
    }

    [Theory]
    [InlineData(true, null, "wrong", "new-secret", "new-secret")]
    [InlineData(true, null, "123", "new-secret", "other")]
    [InlineData(true, null, "123", "123", "123")]
    [InlineData(true, null, "123", " ", " ")]
    [InlineData(true, "old-secret", "wrong", "new-secret", "new-secret")]
    [InlineData(true, "old-secret", "123", "new-secret", "new-secret")]
    [InlineData(true, "old-secret", "old-secret", "new-secret", "other")]
    [InlineData(true, "old-secret", "old-secret", "123", "123")]
    [InlineData(true, "old-secret", "old-secret", "", "")]
    [InlineData(false, null, "123", "new-secret", "new-secret")]
    public async Task ChangePassword_RejectsWrongCurrentPasswordInvalidNewPasswordOrMissingAdministrator(
        bool administratorExists, string? storedPassword, string currentPassword, string newPassword,
        string confirmPassword)
    {
        var context = administratorExists
            ? new IdentityTestContext().WithAdministrator(storedPassword)
            : new IdentityTestContext();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ChangePassword(context, currentPassword, newPassword, confirmPassword)
                .Execute(IdentityTestContext.Executor));

        Assert.Empty(context.Written);
    }

    private static ChangeAdministratorPasswordCommand ChangePassword(IdentityTestContext context,
        string currentPassword, string newPassword, string confirmPassword) =>
        new(context.Handler, IdentityTestContext.Hasher)
        {
            CurrentPassword = currentPassword,
            NewPassword = newPassword,
            ConfirmPassword = confirmPassword
        };

    private static void AssertPasswordMatches(string passwordHash, string password) =>
        Assert.NotEqual(PasswordVerificationResult.Failed, IdentityTestContext.Hasher.VerifyHashedPassword(
            new AccountStateData(IdentityDefaults.AdministratorId), passwordHash, password));
}
