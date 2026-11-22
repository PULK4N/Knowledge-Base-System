using EventSourcing.Shared.Models;
using IdentityModule.Application.Executors;
using IdentityModule.Domain.Events;

namespace IdentityModule.Application.Tests;

public sealed class AccountExecutorReaderTests
{
    private static readonly AggregateId AgentId =
        AggregateId.FromDatabaseGuid(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

    [Fact]
    public async Task Read_Administrator_ReturnsAdministratorExecutor()
    {
        var context = new IdentityTestContext().WithAdministrator("secret");

        var executor = await new AccountExecutorReader(context.Handler).Read(IdentityDefaults.AdministratorId.Value);

        Assert.NotNull(executor);
        Assert.Equal(IdentityDefaults.AdministratorId.Value, executor.Id.Value);
        Assert.True(executor.IsAdministrator);
        Assert.Empty(executor.Permissions);
    }

    [Fact]
    public async Task Read_Agent_ReturnsItsCurrentPermissions()
    {
        var context = new IdentityTestContext().With(AgentId,
            new AgentCreatedV1("codex", "Codex", false, ["memory.write"]),
            new AgentPermissionsSetV1(false, ["skills.read", "features.write"]));

        var executor = await new AccountExecutorReader(context.Handler).Read(AgentId.Value);

        Assert.NotNull(executor);
        Assert.Equal(AgentId.Value, executor.Id.Value);
        Assert.False(executor.IsAdministrator);
        Assert.Equal(["features.write", "skills.read"],
            executor.Permissions.Select(permission => permission.Name).Order().ToList());
    }

    [Fact]
    public async Task Read_UnknownAccount_ReturnsNull()
    {
        var context = new IdentityTestContext().WithAdministrator();

        Assert.Null(await new AccountExecutorReader(context.Handler).Read(AgentId.Value));
    }
}
