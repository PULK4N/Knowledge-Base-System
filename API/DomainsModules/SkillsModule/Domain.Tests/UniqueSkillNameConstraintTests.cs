using EventSourcing.Shared.Models;
using SkillsModule.Domain.Constraints;

namespace SkillsModule.Domain.Tests;

public sealed class UniqueSkillNameConstraintTests
{
    private static readonly AggregateId FirstId =
        AggregateId.FromDatabaseGuid(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly AggregateId SecondId =
        AggregateId.FromDatabaseGuid(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

    private readonly SkillNameConstraintCreator _creator = new();

    [Theory]
    [InlineData("  My-Name  ", "MY-NAME")]
    [InlineData("my-name", "MY-NAME")]
    [InlineData("   ", null)]
    public void Create_ReturnsNormalizedNameConstraint(string name, string? normalizedName)
    {
        var state = new SkillStateData(FirstId) { Name = name };
        List<byte[]> expectedHashes = normalizedName is null
            ? []
            : [new UniqueSkillNameConstraint(state, normalizedName).Hash];

        var constraints = _creator.Create(state);

        Assert.Equal(expectedHashes, constraints.Select(constraint => constraint.Hash));
        Assert.All(
            constraints,
            constraint =>
            {
                Assert.Equal(nameof(UniqueSkillNameConstraint), constraint.ConstraintName);
                Assert.Equal(FirstId.Value, constraint.AggregateId);
            }
        );
    }

    [Fact]
    public void Create_EquivalentNamesOfDifferentAggregates_ShareHash()
    {
        var first = Assert.Single(_creator.Create(new SkillStateData(FirstId) { Name = "My-Name" }));
        var second = Assert.Single(_creator.Create(new SkillStateData(SecondId) { Name = " my-name " }));

        Assert.Equal(first.Hash, second.Hash);
    }
}
