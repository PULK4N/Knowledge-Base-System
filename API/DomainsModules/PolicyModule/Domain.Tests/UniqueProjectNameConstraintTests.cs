using EventSourcing.Shared.Models;
using PolicyModule.Domain.Constraints;

namespace PolicyModule.Domain.Tests;

public sealed class UniqueProjectNameConstraintTests
{
    private static readonly AggregateId FirstId =
        AggregateId.FromDatabaseGuid(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly AggregateId SecondId =
        AggregateId.FromDatabaseGuid(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

    private readonly ProjectNameConstraintCreator _creator = new();

    [Theory]
    [InlineData("  My-Name  ", "MY-NAME")]
    [InlineData("my-name", "MY-NAME")]
    [InlineData("   ", null)]
    public void Create_ReturnsNormalizedNameConstraint(string name, string? normalizedName)
    {
        var state = new ProjectPoliciesStateData(FirstId) { ProjectName = name };
        List<byte[]> expectedHashes = normalizedName is null
            ? []
            : [new UniqueProjectNameConstraint(state, normalizedName).Hash];

        var constraints = _creator.Create(state);

        Assert.Equal(expectedHashes, constraints.Select(constraint => constraint.Hash));
        Assert.All(
            constraints,
            constraint =>
            {
                Assert.Equal(nameof(UniqueProjectNameConstraint), constraint.ConstraintName);
                Assert.Equal(FirstId.Value, constraint.AggregateId);
            }
        );
    }

    [Fact]
    public void Create_EquivalentNamesOfDifferentAggregates_ShareHash()
    {
        var first = Assert.Single(_creator.Create(new ProjectPoliciesStateData(FirstId) { ProjectName = "My-Name" }));
        var second = Assert.Single(_creator.Create(new ProjectPoliciesStateData(SecondId) { ProjectName = " my-name " }));

        Assert.Equal(first.Hash, second.Hash);
    }
}
