using IdentityModule.Application.DTOs;
using IdentityModule.Application.Queries;

namespace IdentityModule.Application.Tests;

public sealed class LoginQueryTests
{
    [Theory]
    [InlineData("admin", "123", true, true)]
    [InlineData(" ADMIN ", "123", true, true)]
    [InlineData("admin", "wrong", false, false)]
    [InlineData("other", "123", false, false)]
    public async Task BeforeAdministratorExists_AcceptsOnlyInitialCredentialsAndAsksToChangeThem(
        string userName, string password, bool succeeded, bool mustChangePassword)
    {
        var context = new IdentityTestContext();

        var result = await Login(context, userName, password);

        Assert.Equal(new LoginResultDto(succeeded, mustChangePassword), result);
    }

    [Theory]
    [InlineData("owner", "secret", true)]
    [InlineData("Owner", "secret", true)]
    [InlineData("owner", "wrong", false)]
    [InlineData("admin", "123", false)]
    [InlineData("other", "secret", false)]
    public async Task AfterAdministratorExists_ChecksStoredUserNameAndPasswordHash(
        string userName, string password, bool succeeded)
    {
        var context = new IdentityTestContext().WithAdministrator("owner", "secret");

        var result = await Login(context, userName, password);

        Assert.Equal(new LoginResultDto(succeeded, MustChangePassword: false), result);
        Assert.Empty(context.Written);
    }

    private static Task<LoginResultDto> Login(IdentityTestContext context, string userName, string password) =>
        new LoginQuery(context.Handler, IdentityTestContext.Hasher) { UserName = userName, Password = password }
            .Execute(IdentityTestContext.Executor);
}
