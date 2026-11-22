using EventSourcing.Shared.Models;
using IdentityModule.Domain.Constraints;

namespace IdentityModule.Domain.Tests;

public sealed class AccountUserNameConstraintTests
{
    private static readonly AggregateId FirstId =
        AggregateId.FromDatabaseGuid(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly AggregateId SecondId =
        AggregateId.FromDatabaseGuid(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

    private readonly AccountUserNameConstraintCreator _creator = new();

    [Theory]
    [InlineData("  Codex  ", "CODEX")]
    [InlineData("admin", "ADMIN")]
    [InlineData("   ", null)]
    public void Create_ReturnsNormalizedUserNameConstraint(string userName, string? normalizedUserName)
    {
        var state = new AccountStateData(FirstId) { UserName = userName };
        List<byte[]> expectedHashes = normalizedUserName is null
            ? []
            : [new UniqueAccountUserNameConstraint(state, normalizedUserName).Hash];

        var constraints = _creator.Create(state);

        Assert.Equal(expectedHashes, constraints.Select(constraint => constraint.Hash));
        Assert.All(constraints, constraint => Assert.Equal(FirstId.Value, constraint.AggregateId));
    }

    [Fact]
    public void AdministratorAndAgentWithEquivalentUserNames_ShareHash()
    {
        var administrator = Assert.Single(_creator.Create(
            new AccountStateData(FirstId) { UserName = "admin" }));
        var agent = Assert.Single(_creator.Create(
            new AccountStateData(SecondId) { UserName = " ADMIN ", IsAgent = true }));

        Assert.Equal(administrator.Hash, agent.Hash);
    }
}
