using EventSourcing.Shared.Models;
using IdentityModule.Application.DTOs;
using IdentityModule.Domain;
using Microsoft.EntityFrameworkCore;

namespace IdentityModule.Persistence.Tests;

public sealed class AccountListProjectorTests
{
    private static readonly AggregateId AdministratorId =
        AggregateId.FromDatabaseGuid(Guid.Parse("00000000-0000-0000-0000-000000000005"));
    private static readonly AggregateId CodexId =
        AggregateId.FromDatabaseGuid(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly AggregateId ClaudeId =
        AggregateId.FromDatabaseGuid(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly DateTime Earlier = new(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Later = new(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Projection_StoresAccountsWithoutPasswordsAndListsOnlyAgentsNewestFirst()
    {
        await using var context = TestIdentityDbContext.Create();
        var repository = new AccountListRepository(context);

        await new AccountListProjector(repository).Update(
        [
            StateInfo(Administrator(), 1, Later),
            StateInfo(Agent(CodexId, "codex", ["memory.write"]), 1, Earlier),
            StateInfo(Agent(ClaudeId, "claude", ["skills.read"], isAdministrator: true), 2, Later)
        ]);

        Assert.Equal(3, await context.AccountListEntries.CountAsync());
        Assert.Equal(
        [
            $"{ClaudeId.Value}|claude|claude agent|True|skills.read",
            $"{CodexId.Value}|codex|codex agent|False|memory.write"
        ], (await repository.ListAgents()).Select(Describe));
    }

    [Fact]
    public async Task ProjectingTheSameStateAgain_LeavesTheRowsUnchanged()
    {
        await using var context = TestIdentityDbContext.Create();
        var repository = new AccountListRepository(context);
        List<StateInfo> stateInfos =
        [
            StateInfo(Agent(CodexId, "codex", ["memory.write"]), 1, Earlier),
            StateInfo(Agent(ClaudeId, "claude", ["skills.read"]), 1, Earlier)
        ];

        await new AccountListProjector(repository).Update(stateInfos);
        var first = await Rows(context);
        await new AccountListProjector(repository).Update(stateInfos);

        Assert.Equal(first, await Rows(context));
    }

    [Fact]
    public async Task OlderState_DoesNotOverwriteNewerRow()
    {
        await using var context = TestIdentityDbContext.Create();
        var repository = new AccountListRepository(context);

        await new AccountListProjector(repository).Update(
            [StateInfo(Agent(CodexId, "codex", ["features.write"]), 2, Later)]);
        await new AccountListProjector(repository).Update(
            [StateInfo(Agent(CodexId, "codex", ["memory.write"]), 1, Earlier)]);

        Assert.Equal(["features.write"], Assert.Single(await repository.ListAgents()).Permissions);
    }

    [Fact]
    public async Task DeletedAccount_RemovesItsRow()
    {
        await using var context = TestIdentityDbContext.Create();
        var repository = new AccountListRepository(context);
        var codex = Agent(CodexId, "codex", ["memory.write"]);
        await new AccountListProjector(repository).Update([StateInfo(codex, 1, Earlier)]);

        codex.IsDeleted = true;
        await new AccountListProjector(repository).Update([StateInfo(codex, 2, Later)]);

        Assert.Empty(context.AccountListEntries);
    }

    private static Task<List<string>> Rows(TestIdentityDbContext context) =>
        context.AccountListEntries.AsNoTracking().OrderBy(entry => entry.UserName)
            .Select(entry => $"{entry.AccountId}|{entry.UserName}|{entry.DisplayName}|{entry.IsAdministrator}|"
                             + $"{entry.IsAgent}|{string.Join(',', entry.Permissions)}|{entry.UpdatedAt:O}|"
                             + $"{entry.ProjectedOrderNumber}")
            .ToListAsync();

    private static AccountStateData Administrator() => new(AdministratorId)
    {
        IsCreated = true,
        IsAdministrator = true,
        UserName = "admin",
        DisplayName = "admin",
        PasswordHash = "secret-hash"
    };

    private static AccountStateData Agent(
        AggregateId id, string userName, List<string> permissions, bool isAdministrator = false) => new(id)
    {
        IsCreated = true,
        IsAgent = true,
        UserName = userName,
        DisplayName = $"{userName} agent",
        IsAdministrator = isAdministrator,
        Permissions = permissions
    };

    private static StateInfo StateInfo(AccountStateData account, uint orderNumber, DateTime updatedAt)
    {
        var stateInfo = EventSourcing.Shared.Models.StateInfo.Create(account, "identity-state-machine", account.Id);
        stateInfo.CurrentOrderNumber = orderNumber;
        stateInfo.LastUpdateTimestamp = updatedAt;
        return stateInfo;
    }

    private static string Describe(AgentDto agent) =>
        $"{agent.Id}|{agent.UserName}|{agent.DisplayName}|{agent.IsAdministrator}|{string.Join(',', agent.Permissions)}";
}
