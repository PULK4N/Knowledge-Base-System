using ActionModule.Shared.Models;
using EventSourcing.Shared.Exceptions;
using EventSourcing.Shared.Models;
using IdentityModule.Application.Commands;
using IdentityModule.Application.Queries;
using IdentityModule.Domain.Events;
using IdentityModule.Shared;

namespace IdentityModule.Application.Tests;

public sealed class AgentCommandTests
{
    private static readonly AggregateId AgentId =
        AggregateId.FromDatabaseGuid(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

    [Theory]
    [InlineData(" Codex agent ", "Codex agent")]
    [InlineData(null, "codex")]
    [InlineData(" ", "codex")]
    public async Task CreateAgent_WritesAgentCreatedAndReturnsTheAgent(string? displayName, string expectedDisplayName)
    {
        var context = new IdentityTestContext();

        var agent = await new CreateAgentCommand(context.Handler)
        {
            UserName = " codex ",
            DisplayName = displayName,
            Permissions = ["memory.write", "skills.read", "memory.write"]
        }.Execute(IdentityTestContext.Executor);

        var payload = Assert.Single(context.Written);
        var created = Assert.IsType<AgentCreatedV1>(payload.EventData);
        Assert.Equal("codex", created.UserName);
        Assert.Equal(expectedDisplayName, created.DisplayName);
        Assert.False(created.IsAdministrator);
        Assert.Equal(["memory.write", "skills.read"], created.Permissions.ToList());
        Assert.Equal(payload.EventExecutionInfo.AggregateId.Value, agent.Id);
        Assert.Equal(("codex", expectedDisplayName, false), (agent.UserName, agent.DisplayName, agent.IsAdministrator));
        Assert.Equal(["memory.write", "skills.read"], agent.Permissions);
    }

    [Fact]
    public async Task CreateAgent_WithUnknownPermission_IsRejectedAndWritesNothing()
    {
        var context = new IdentityTestContext();

        await Assert.ThrowsAsync<EventValidationException>(() => new CreateAgentCommand(context.Handler)
        {
            UserName = "codex",
            Permissions = ["everything"]
        }.Execute(IdentityTestContext.Executor));

        Assert.Empty(context.Written);
    }

    [Fact]
    public async Task SetAgentPermissions_WritesNewPermissionsAndReturnsTheAgent()
    {
        var context = new IdentityTestContext()
            .With(AgentId, new AgentCreatedV1("codex", "Codex", false, ["memory.write"]));

        var agent = await new SetAgentPermissionsCommand(context.Handler)
        {
            AgentId = AgentId.Value,
            IsAdministrator = true,
            Permissions = ["features.write"]
        }.Execute(IdentityTestContext.Executor);

        var permissionsSet = Assert.IsType<AgentPermissionsSetV1>(Assert.Single(context.Written).EventData);
        Assert.True(permissionsSet.IsAdministrator);
        Assert.Equal(["features.write"], permissionsSet.Permissions.ToList());
        Assert.Equal(AgentId.Value, agent.Id);
        Assert.True(agent.IsAdministrator);
        Assert.Equal(["features.write"], agent.Permissions);
    }

    [Fact]
    public async Task SetAgentPermissions_OnTheAdministratorAccount_IsRejectedAndWritesNothing()
    {
        var context = new IdentityTestContext().WithAdministrator();

        await Assert.ThrowsAsync<EventValidationException>(() => new SetAgentPermissionsCommand(context.Handler)
        {
            AgentId = IdentityDefaults.AdministratorId.Value,
            Permissions = ["features.write"]
        }.Execute(IdentityTestContext.Executor));

        Assert.Empty(context.Written);
    }

    [Fact]
    public async Task AgentActions_ByANonAdministrator_AreRejectedAndWriteNothing()
    {
        var context = new IdentityTestContext()
            .With(AgentId, new AgentCreatedV1("codex", "Codex", false, ["memory.write"]));
        var agent = new Executor
        {
            Id = EventExecutor.FromDatabaseGuid(AgentId.Value),
            Permissions = Permissions.All.ToHashSet()
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            new CreateAgentCommand(context.Handler) { UserName = "other" }.Execute(agent));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            new SetAgentPermissionsCommand(context.Handler) { AgentId = AgentId.Value, IsAdministrator = true }
                .Execute(agent));
        // IsAuthorized only looks at the executor, so the query needs no repository here.
        Assert.False(await new ListAgentsQuery(null!).IsAuthorized(agent));
        Assert.Empty(context.Written);
    }
}
