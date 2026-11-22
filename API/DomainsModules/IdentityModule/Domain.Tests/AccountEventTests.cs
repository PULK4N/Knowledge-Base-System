using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using IdentityModule.Domain.Events;

namespace IdentityModule.Domain.Tests;

public sealed class AccountEventTests
{
    private static readonly AggregateId AccountId =
        AggregateId.FromDatabaseGuid(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

    public static TheoryData<IEvent[], string> AdministratorHistories => new()
    {
        { [new AdministratorCreatedV1("admin")], "" },
        { [new AdministratorCreatedV1("admin"), new AdministratorPasswordChangedV1("hash-1")], "hash-1" },
        {
            [
                new AdministratorCreatedV1("admin"),
                new AdministratorPasswordChangedV1("hash-1"),
                new AdministratorPasswordChangedV1("hash-2")
            ],
            "hash-2"
        }
    };

    [Theory]
    [MemberData(nameof(AdministratorHistories))]
    public void AdministratorEvents_ReplayToAdministratorWithLatestPasswordHash(
        IEvent[] events, string passwordHash)
    {
        var account = IdentityStateMachine.Replay(AccountId, events);

        Assert.True(account.IsCreated);
        Assert.True(account.IsAdministrator);
        Assert.False(account.IsAgent);
        Assert.Equal("admin", account.UserName);
        Assert.Equal("admin", account.DisplayName);
        Assert.Equal(passwordHash, account.PasswordHash);
        Assert.Empty(account.Permissions);
    }

    public static TheoryData<IEvent[], bool, string[]> AgentHistories => new()
    {
        { [new AgentCreatedV1("codex", "Codex", false, ["memory.write"])], false, ["memory.write"] },
        { [new AgentCreatedV1("codex", "Codex", true, [])], true, [] },
        {
            [
                new AgentCreatedV1("codex", "Codex", false, ["memory.write"]),
                new AgentPermissionsSetV1(false, ["skills.read", "features.write"])
            ],
            false, ["skills.read", "features.write"]
        },
        {
            [
                new AgentCreatedV1("codex", "Codex", false, ["memory.write"]),
                new AgentPermissionsSetV1(true, [])
            ],
            true, []
        }
    };

    [Theory]
    [MemberData(nameof(AgentHistories))]
    public void AgentEvents_ReplayToAgentWithLatestPermissions(
        IEvent[] events, bool isAdministrator, string[] permissions)
    {
        var account = IdentityStateMachine.Replay(AccountId, events);

        Assert.True(account.IsCreated);
        Assert.True(account.IsAgent);
        Assert.Equal("codex", account.UserName);
        Assert.Equal("Codex", account.DisplayName);
        Assert.Equal(isAdministrator, account.IsAdministrator);
        Assert.Equal(permissions, account.Permissions);
        Assert.Empty(account.PasswordHash);
    }
}
