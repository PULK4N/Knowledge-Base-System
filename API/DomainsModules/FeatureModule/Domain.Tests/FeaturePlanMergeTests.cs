using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Tests;

public sealed class FeaturePlanMergeTests
{
    private static readonly FeaturePlanId X1 = new(Guid.NewGuid());
    private static readonly FeaturePlanId X2 = new(Guid.NewGuid());
    private static readonly FeaturePlanId X3 = new(Guid.NewGuid());
    private static readonly FeaturePlanId X4 = new(Guid.NewGuid());

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Merging_x2_into_x1_makes_x3_come_from_x1(bool explicitSources)
    {
        // x1 <- x2 <- x3, by previous plan or by explicit sources.
        var plans = new List<FeaturePlan>
        {
            Plan(X1, 1, null, Base("x1")),
            Plan(X2, 2, X1, explicitSources ? Base("x2", Source(X1, "x1")) : Base("x2")),
            Plan(X3, 3, X2, explicitSources ? Base("x3", Source(X2, "x2")) : Base("x3"))
        };

        var rewrites = FeaturePlanMerge.RewriteFollowers(plans, X2, Merged());

        Assert.Equal(["x3 <- x1"], Describe(rewrites));
    }

    [Theory]
    [InlineData(null, "x3 <- x1-api, x1-web")]
    [InlineData("x1-web", "x3 <- x1-web")]
    [InlineData("feature", "x3 <- main")]
    public void A_base_branch_is_merged_into_the_chosen_branch_or_its_own_sources(
        string? into,
        string expected
    )
    {
        var plans = new List<FeaturePlan>
        {
            Plan(X1, 1, null, Base("x1-api"), Base("x1-web")),
            Plan(X2, 2, null, Base("x2", Source(X1, "x1-api"), Source(X1, "x1-web"))),
            Plan(X3, 3, X2, Base("x3"))
        };
        var mergedInto = into switch
        {
            null => Merged(),
            "feature" => Merged(("x2", FeaturePlanBranchSource.FeatureBranch)),
            _ => Merged(("x2", Source(X1, into)))
        };

        var rewrites = FeaturePlanMerge.RewriteFollowers(plans, X2, mergedInto);

        Assert.Equal([expected], Describe(rewrites));
    }

    [Fact]
    public void A_sub_branch_counts_as_merged_with_its_base_branch_and_other_sources_stay()
    {
        var plans = new List<FeaturePlan>
        {
            Plan(X1, 1, null, Base("x1")),
            Plan(X2, 2, X1, Base("x2", Sub("x2-tests"))),
            Plan(X4, 3, null, Base("x4")),
            Plan(X3, 4, null, Base("x3", Source(X2, "x2-tests"), Source(X4, "x4"))),
            Plan(new FeaturePlanId(Guid.NewGuid()), 5, X4, Base("untouched"))
        };

        var rewrites = FeaturePlanMerge.RewriteFollowers(plans, X2, Merged());

        Assert.Equal(["x3 <- x1, x4"], Describe(rewrites));
    }

    [Fact]
    public void Each_base_branch_of_the_completed_plan_can_go_to_a_different_branch()
    {
        // x3 came from x2-api and x2-web; they were merged into x1-api and x4.
        var plans = new List<FeaturePlan>
        {
            Plan(X1, 1, null, Base("x1-api")),
            Plan(X4, 2, null, Base("x4")),
            Plan(X2, 3, X1, Base("x2-api"), Base("x2-web")),
            Plan(X3, 4, null, Base("x3", Source(X2, "x2-api"), Source(X2, "x2-web")))
        };

        var rewrites = FeaturePlanMerge.RewriteFollowers(
            plans,
            X2,
            Merged(("x2-web", Source(X4, "x4")))
        );

        Assert.Equal(["x3 <- x1-api, x4"], Describe(rewrites));
    }

    [Theory]
    [InlineData("x2-missing", "x1", "base branch")]
    [InlineData("x2", "own", "own plan")]
    [InlineData("x2", "x1-missing", "does not exist")]
    public void Merge_targets_must_be_valid(string branchName, string into, string message)
    {
        var plans = new List<FeaturePlan>
        {
            Plan(X1, 1, null, Base("x1")),
            Plan(X2, 2, X1, Base("x2"), Base("x2-web"))
        };
        var target = into == "own" ? Source(X2, "x2-web") : Source(X1, into);

        var error = Assert.Throws<InvalidOperationException>(
            () => FeaturePlanMerge.RewriteFollowers(plans, X2, Merged((branchName, target)))
        );

        Assert.Contains(message, error.Message);
    }

    private static List<string> Describe(
        List<(FeaturePlanId PlanId, string BranchName, List<FeaturePlanBranchSource> Sources)> rewrites
    ) =>
        rewrites
            .Select(rewrite => $"{rewrite.BranchName} <- {string.Join(", ", rewrite.Sources.Select(source => source.PlanId is null ? "main" : source.BranchName))}")
            .ToList();

    private static Dictionary<string, FeaturePlanBranchSource> Merged(
        params (string BranchName, FeaturePlanBranchSource Into)[] merges
    ) =>
        merges.ToDictionary(merge => merge.BranchName, merge => merge.Into);

    private static FeaturePlan Plan(
        FeaturePlanId id,
        int day,
        FeaturePlanId? previousPlanId,
        params FeaturePlanBranch[] branches
    ) =>
        new()
        {
            Id = id,
            PreviousPlanId = previousPlanId,
            Branches = [.. branches],
            CreatedAt = DateTime.UnixEpoch.AddDays(day)
        };

    private static FeaturePlanBranch Base(string name, params FeaturePlanBranchSource[] sources) =>
        new() { Name = name, Sources = [.. sources] };

    private static FeaturePlanBranch Base(string name, FeaturePlanBranch subBranch) =>
        new() { Name = name, SubBranches = [subBranch] };

    private static FeaturePlanBranch Sub(string name) => new() { Name = name };

    private static FeaturePlanBranchSource Source(FeaturePlanId planId, string branchName) =>
        new(planId, branchName);
}
