using EventSourcing.Shared.Models;
using FeatureModule.Domain.Constraints;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Tests;

public sealed class UniquePlanBranchNameConstraintTests
{
    private static readonly AggregateId ProjectId =
        AggregateId.FromDatabaseGuid(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));

    private readonly PlanBranchNameConstraintCreator _creator = new();

    [Fact]
    public void Create_ReservesEveryPlanBranchOnceIncludingSubBranches()
    {
        var state = State(ProjectId,
            Plan(Branch("feature/x", Branch("feature/x-api"))),
            Plan(Branch(" feature/y "), Branch("feature/x"), Branch("  ")));

        var constraints = _creator.Create(state);

        Assert.Equal(3, constraints.Count);
        Assert.Equal(3, constraints.Select(constraint => Convert.ToHexString(constraint.Hash)).Distinct().Count());
        Assert.All(constraints, constraint =>
        {
            Assert.Equal(nameof(UniquePlanBranchNameConstraint), constraint.ConstraintName);
            Assert.Equal(state.Id.Value, constraint.AggregateId);
        });
    }

    [Theory]
    [InlineData(true, "feature/x", true)]
    [InlineData(true, " feature/x ", true)]
    [InlineData(true, "Feature/X", false)]
    [InlineData(false, "feature/x", false)]
    public void Create_SameBranchInSameProjectSharesHash(bool sameProject, string otherName, bool sameHash)
    {
        var otherProject = sameProject ? ProjectId : AggregateId.FromDatabaseGuid(Guid.NewGuid());
        var first = Assert.Single(_creator.Create(State(ProjectId, Plan(Branch("feature/x")))));
        var second = Assert.Single(_creator.Create(State(otherProject, Plan(Branch(otherName)))));

        Assert.Equal(sameHash, first.Hash.SequenceEqual(second.Hash));
    }

    private static FeatureStateData State(AggregateId projectId, params FeaturePlan[] plans) =>
        new(AggregateId.FromDatabaseGuid(Guid.NewGuid())) { ProjectId = projectId, Plans = [.. plans] };

    private static FeaturePlan Plan(params FeaturePlanBranch[] branches) => new()
    {
        Id = new FeaturePlanId(Guid.NewGuid()), Title = "Plan", Content = "content", Branches = [.. branches]
    };

    private static FeaturePlanBranch Branch(string name, params FeaturePlanBranch[] subBranches) =>
        new() { Name = name, SubBranches = [.. subBranches] };
}
