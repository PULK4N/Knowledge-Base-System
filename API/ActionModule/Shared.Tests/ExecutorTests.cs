using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;

namespace ActionModule.Shared.Tests;

public sealed class ExecutorTests
{
    [Theory]
    [InlineData(true, new string[0], "skills.write", true)]
    [InlineData(true, new string[0], "anything", true)]
    [InlineData(false, new[] { "skills.write" }, "skills.write", true)]
    [InlineData(false, new[] { "skills.write" }, "skills.read", false)]
    [InlineData(false, new[] { "skills.read" }, "skills.read", true)]
    [InlineData(false, new[] { "skills.read" }, "skills.write", false)]
    [InlineData(false, new[] { "skills.write" }, "memory.read", false)]
    [InlineData(false, new[] { "skills.write" }, "memory.write", false)]
    [InlineData(false, new string[0], "skills.read", false)]
    public void Has_GrantsEverythingToAdministratorAndOnlyGrantedPermissionsToOthers(
        bool isAdministrator, string[] permissions, string permission, bool expected)
    {
        var executor = new Executor
        {
            Id = EventExecutor.FromDatabaseGuid(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
            IsAdministrator = isAdministrator,
            Permissions = [.. permissions.Select(name => new Permission(name))]
        };

        Assert.Equal(expected, executor.Has(new Permission(permission)));
    }
}
