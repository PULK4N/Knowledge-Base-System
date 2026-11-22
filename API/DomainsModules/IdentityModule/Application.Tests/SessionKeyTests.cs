using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;
using IdentityModule.Application.Commands;
using IdentityModule.Application.SessionKeys;
using IdentityModule.Domain.Events;
using Microsoft.AspNetCore.DataProtection;

namespace IdentityModule.Application.Tests;

public sealed class SessionKeyTests
{
    private static readonly AggregateId AgentId =
        AggregateId.FromDatabaseGuid(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

    private static readonly Executor Administrator = new()
    {
        Id = EventExecutor.FromDatabaseGuid(IdentityDefaults.AdministratorId.Value),
        IsAdministrator = true
    };

    private readonly SessionKeyProtector _protector = new(new EphemeralDataProtectionProvider());

    [Fact]
    public async Task GenerateSessionKey_ForAnAgent_ReturnsAKeyNamingTheAgentAndWritesNothing()
    {
        var context = AgentContext();

        var result = await Generate(context, AgentId.Value).Execute(Administrator);

        Assert.True(_protector.TryUnprotect(result.SessionKey, out var accountId));
        Assert.Equal(AgentId.Value, accountId);
        Assert.DoesNotContain(AgentId.Value.ToString(), result.SessionKey);
        Assert.Empty(context.Written);
    }

    [Fact]
    public async Task GenerateSessionKey_ByANonAdministrator_IsRejected()
    {
        var context = AgentContext();
        var agentExecutor = new Executor
        {
            Id = EventExecutor.FromDatabaseGuid(AgentId.Value),
            Permissions = [new Permission("skills.write")]
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            Generate(context, AgentId.Value).Execute(agentExecutor));
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000005")]
    [InlineData("cccccccc-cccc-cccc-cccc-cccccccccccc")]
    public async Task GenerateSessionKey_ForTheAdministratorOrAnUnknownAccount_IsRejected(string accountId)
    {
        var context = AgentContext().WithAdministrator("secret");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Generate(context, Guid.Parse(accountId)).Execute(Administrator));
    }

    [Fact]
    public void TryUnprotect_RejectsKeysNotIssuedHere()
    {
        var otherKnowledgeBase = new SessionKeyProtector(new EphemeralDataProtectionProvider());
        var otherPurpose = new EphemeralDataProtectionProvider().CreateProtector("other");
        var issued = _protector.Protect(AgentId.Value);
        var changed = issued[..^2] + (issued[^2] == 'A' ? "B" : "A") + issued[^1];

        List<string> rejected =
        [
            "not a key",
            "",
            changed,
            otherKnowledgeBase.Protect(AgentId.Value),
            otherPurpose.Protect(AgentId.Value.ToString())
        ];

        Assert.All(rejected, key => Assert.False(_protector.TryUnprotect(key, out _)));
    }

    private static IdentityTestContext AgentContext() =>
        new IdentityTestContext().With(AgentId, new AgentCreatedV1("codex", "Codex", false, ["skills.read"]));

    private GenerateSessionKeyCommand Generate(IdentityTestContext context, Guid agentId) =>
        new(context.Handler, _protector) { AgentId = agentId };
}
