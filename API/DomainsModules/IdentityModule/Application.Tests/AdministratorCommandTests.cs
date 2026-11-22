using IdentityModule.Application.Commands;
using IdentityModule.Application.DTOs;
using IdentityModule.Domain;
using IdentityModule.Domain.Events;
using Microsoft.AspNetCore.Identity;

namespace IdentityModule.Application.Tests;

public sealed class AdministratorCommandTests
{
    [Theory]
    [InlineData(null, "admin")]
    [InlineData("  ", "admin")]
    [InlineData(" owner ", "owner")]
    public async Task SetInitialCredentials_CreatesAdministratorWithHashedNewPassword(
        string? newUserName, string expectedUserName)
    {
        var context = new IdentityTestContext();

        var result = await InitialCredentials(context, "123", newUserName, "secret", "secret")
            .Execute(IdentityTestContext.Executor);

        Assert.Equal(new LoginResultDto(true, MustChangePassword: false), result);
        var payload = Assert.Single(context.Written);
        Assert.Equal(IdentityDefaults.AdministratorId, payload.EventExecutionInfo.AggregateId);
        Assert.Equal(IdentityTestContext.Executor.Id, payload.EventExecutionInfo.EventExecutor);
        var created = Assert.IsType<AdministratorCreatedV1>(payload.EventData);
        Assert.Equal(expectedUserName, created.UserName);
        Assert.DoesNotContain("secret", created.PasswordHash);
        AssertPasswordMatches(created.PasswordHash, "secret");
    }

    [Theory]
    [InlineData("wrong", "secret", "secret", false)]
    [InlineData("123", "secret", "other", false)]
    [InlineData("123", "123", "123", false)]
    [InlineData("123", " ", " ", false)]
    [InlineData("123", "secret", "secret", true)]
    public async Task SetInitialCredentials_RejectsInvalidPasswordsOrAnExistingAdministrator(
        string currentPassword, string newPassword, string confirmPassword, bool administratorExists)
    {
        var context = administratorExists
            ? new IdentityTestContext().WithAdministrator("admin", "existing")
            : new IdentityTestContext();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            InitialCredentials(context, currentPassword, null, newPassword, confirmPassword)
                .Execute(IdentityTestContext.Executor));

        Assert.Empty(context.Written);
    }

    [Fact]
    public async Task ChangePassword_WritesNewHashForTheAdministrator()
    {
        var context = new IdentityTestContext().WithAdministrator("admin", "old-secret");

        Assert.True(await ChangePassword(context, "old-secret", "new-secret", "new-secret")
            .Execute(IdentityTestContext.Executor));

        var payload = Assert.Single(context.Written);
        Assert.Equal(IdentityDefaults.AdministratorId, payload.EventExecutionInfo.AggregateId);
        AssertPasswordMatches(Assert.IsType<AdministratorPasswordChangedV1>(payload.EventData).PasswordHash,
            "new-secret");
    }

    [Theory]
    [InlineData("wrong", "new-secret", "new-secret", true)]
    [InlineData("old-secret", "new-secret", "other", true)]
    [InlineData("old-secret", "123", "123", true)]
    [InlineData("old-secret", "", "", true)]
    [InlineData("old-secret", "new-secret", "new-secret", false)]
    public async Task ChangePassword_RejectsWrongCurrentPasswordInvalidNewPasswordOrMissingAdministrator(
        string currentPassword, string newPassword, string confirmPassword, bool administratorExists)
    {
        var context = administratorExists
            ? new IdentityTestContext().WithAdministrator("admin", "old-secret")
            : new IdentityTestContext();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ChangePassword(context, currentPassword, newPassword, confirmPassword)
                .Execute(IdentityTestContext.Executor));

        Assert.Empty(context.Written);
    }

    private static SetInitialCredentialsCommand InitialCredentials(IdentityTestContext context,
        string currentPassword, string? newUserName, string newPassword, string confirmPassword) =>
        new(context.Handler, IdentityTestContext.Hasher)
        {
            CurrentPassword = currentPassword,
            NewUserName = newUserName,
            NewPassword = newPassword,
            ConfirmPassword = confirmPassword
        };

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
