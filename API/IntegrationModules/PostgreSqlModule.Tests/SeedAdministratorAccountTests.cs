using EventSourcing.Shared.Models;
using IdentityModule.Domain;
using IdentityModule.Domain.Constraints;
using PostgreSqlModule.Migrations.EventSourcing;
using SharedModule.Constants;
using Xunit;

namespace PostgreSqlModule.Tests;

public sealed class SeedAdministratorAccountTests
{
    [Fact]
    public void UserNameConstraintHash_MatchesTheConstraintTheStateMachineBuildsForAdmin()
    {
        var administrator =
            new AccountStateData(AggregateId.FromDatabaseGuid(StateDataAggregateIds.AdministratorAccount))
            {
                UserName = "admin"
            };

        var constraint = Assert.Single(new AccountUserNameConstraintCreator().Create(administrator));

        Assert.Equal(nameof(UniqueAccountUserNameConstraint), constraint.ConstraintName);
        Assert.Equal(SeedAdministratorAccount.AdministratorUserNameConstraintHash,
            Convert.ToHexString(constraint.Hash).ToLowerInvariant());
    }
}
