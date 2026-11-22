using IdentityModule.Application.DTOs;
using IdentityModule.Application.Queries;

namespace IdentityModule.Application.Tests;

public sealed class LoginQueryTests
{
    [Theory]
    [InlineData(null, "admin", "123", true, true)]
    [InlineData(null, " ADMIN ", "123", true, true)]
    [InlineData(null, "admin", "wrong", false, false)]
    [InlineData(null, "other", "123", false, false)]
    [InlineData("secret", "admin", "secret", true, false)]
    [InlineData("secret", "Admin", "secret", true, false)]
    [InlineData("secret", "admin", "wrong", false, false)]
    [InlineData("secret", "admin", "123", false, false)]
    [InlineData("secret", "other", "secret", false, false)]
    public async Task Login_ChecksAdministratorAndAsksToChangeTheInitialPassword(
        string? storedPassword, string userName, string password, bool succeeded, bool mustChangePassword)
    {
        var context = new IdentityTestContext().WithAdministrator(storedPassword);

        var result = await Login(context, userName, password);

        Assert.Equal(new LoginResultDto(succeeded, mustChangePassword), result);
        Assert.Empty(context.Written);
    }

    [Fact]
    public async Task Login_WithoutSeededAdministrator_Fails()
    {
        var result = await Login(new IdentityTestContext(), "admin", "123");

        Assert.Equal(LoginResultDto.Failed, result);
    }

    private static Task<LoginResultDto> Login(IdentityTestContext context, string userName, string password) =>
        new LoginQuery(context.Handler, IdentityTestContext.Hasher) { UserName = userName, Password = password }
            .Execute(IdentityTestContext.Executor);
}
