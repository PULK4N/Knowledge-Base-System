using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using FeatureModule.Domain.Validators;

namespace FeatureModule.Domain.Tests;

public sealed class FeatureBranchGraphTests
{
    private static readonly AggregateId FeatureId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly AggregateId MemoryId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly FeaturePlanId X1 = new(Guid.NewGuid());
    private static readonly FeaturePlanId X2 = new(Guid.NewGuid());
    private static readonly FeaturePlanId X3 = new(Guid.NewGuid());

    [Fact]
    public void A_base_branch_can_come_from_two_branches_of_one_plan()
    {
        // main ──► x1-api ──┐
        //    └───► x1-web ──┴──► x2
        var state = State(
            Plan(X1, 1, null, Base("x1-api"), Base("x1-web")),
            Plan(X2, 2, X1, Base("x2", Source(X1, "x1-api"), Source(X1, "x1-web")))
        );

        Assert.Equal(
            [
                "x1-api <- main",
                "x1-web <- main",
                "x2 <- x1-api, x1-web"
            ],
            Describe(FeatureBranchGraph.Build(state.Plans))
        );
    }

    [Fact]
    public void A_base_branch_can_come_from_branches_of_different_plans()
    {
        var state = State(
            Plan(X1, 1, null, Base("x1-api")),
            Plan(X3, 2, null, Base("x3-web")),
            Plan(X2, 3, X1, Base("x2", Source(X1, "x1-api"), Source(X3, "x3-web")))
        );

        Assert.Equal(
            [
                "x1-api <- main",
                "x3-web <- main",
                "x2 <- x1-api, x3-web"
            ],
            Describe(FeatureBranchGraph.Build(state.Plans))
        );
    }

    [Fact]
    public void Without_sources_a_base_branch_comes_from_the_previous_plan_or_the_feature_branch()
    {
        var state = State(
            Plan(X1, 1, null, Base("x1-api", new FeaturePlanBranch { Name = "x1-api-tests" }), Base("x1-web")),
            Plan(X3, 2, X1),
            Plan(X2, 3, X3, Base("x2"))
        );

        Assert.Equal(
            [
                "x1-api <- main",
                "x1-api-tests <- x1-api",
                "x1-web <- main",
                "x2 <- x1-api"
            ],
            Describe(FeatureBranchGraph.Build(state.Plans))
        );
    }

    [Theory]
    [InlineData("feature", true)]
    [InlineData("other-plan", true)]
    [InlineData("sub-branch", true)]
    [InlineData("own-plan", false)]
    [InlineData("missing", false)]
    [InlineData("duplicate", false)]
    [InlineData("cycle", false)]
    [InlineData("missing-base", false)]
    public void Sources_validator_accepts_only_existing_branches_without_cycles(
        string sources,
        bool succeeds
    )
    {
        var state = State(
            Plan(X1, 1, null, Base("x1-api", new FeaturePlanBranch { Name = "x1-api-tests" })),
            Plan(X2, 2, X1, Base("x2"))
        );
        List<FeaturePlanBranchSource> proposed = sources switch
        {
            "feature" => [FeaturePlanBranchSource.FeatureBranch],
            "other-plan" => [Source(X1, "x1-api")],
            "sub-branch" => [Source(X1, "x1-api-tests")],
            "own-plan" => [Source(X2, "x2")],
            "missing" => [Source(X1, "x1-missing")],
            "duplicate" => [Source(X1, "x1-api"), Source(X1, "x1-api")],
            _ => [Source(X2, "x2")]
        };
        var (planId, branchName) = sources switch
        {
            "cycle" => (X1, "x1-api"),
            "missing-base" => (X2, "x2-missing"),
            _ => (X2, "x2")
        };

        var result = new FeaturePlanBranchSourcesValidator().Validate(
            state,
            Payload(new FeaturePlanBaseBranchSourcesSetV1(planId, branchName, [.. proposed], Guid.Empty, MemoryId))
        );

        Assert.Equal(succeeds, result.Succeded);
    }

    [Fact]
    public void Previous_plan_validator_rejects_a_previous_plan_that_makes_branches_loop()
    {
        // x1-api comes from x2, so x2 cannot start from x1 by default.
        var state = State(
            Plan(X1, 1, null, Base("x1-api", Source(X2, "x2"))),
            Plan(X2, 2, null, Base("x2"))
        );

        var result = new FeaturePreviousPlanValidator().Validate(
            state,
            Payload(new FeaturePlanPreviousPlanSetV1(X2, X1, Guid.Empty, MemoryId))
        );

        Assert.False(result.Succeded);
    }

    [Theory]
    [InlineData("rename", "x2 <- x1-api-v2, x3-web")]
    [InlineData("remove-branch", "x2 <- x3-web")]
    [InlineData("remove-plan", "x2 <- x1-api")]
    public void Renaming_or_removing_a_source_updates_other_plans(
        string change,
        string expected
    )
    {
        var state = State(
            Plan(X1, 1, null, Base("x1-api"), Base("x1-web")),
            Plan(X3, 2, null, Base("x3-web")),
            Plan(X2, 3, null, Base("x2", Source(X1, "x1-api"), Source(X3, "x3-web")))
        );
        IEvent eventData = change switch
        {
            "rename" => new FeaturePlanBranchUpdatedV1(X1, "x1-api", "x1-api-v2", "", Guid.Empty, MemoryId),
            "remove-branch" => new FeaturePlanBranchRemovedV1(X1, "x1-api", Guid.Empty, MemoryId),
            _ => new FeaturePlanRemovedV2(X3, Guid.Empty, MemoryId)
        };

        eventData.Apply(state, Payload(eventData).EventExecutionInfo);

        Assert.Contains(expected, Describe(FeatureBranchGraph.Build(state.Plans)));
    }

    [Fact]
    public void Base_branch_sources_set_v1_replaces_and_clears_sources()
    {
        var state = State(
            Plan(X1, 1, null, Base("x1-api")),
            Plan(X2, 2, X1, Base("x2"))
        );
        IEvent set = new FeaturePlanBaseBranchSourcesSetV1(X2, "x2", [FeaturePlanBranchSource.FeatureBranch], Guid.Empty, MemoryId);
        IEvent cleared = new FeaturePlanBaseBranchSourcesSetV1(X2, "x2", [], Guid.Empty, MemoryId);

        set.Apply(state, Payload(set).EventExecutionInfo);
        Assert.Contains("x2 <- main", Describe(FeatureBranchGraph.Build(state.Plans)));

        cleared.Apply(state, Payload(cleared).EventExecutionInfo);
        Assert.Contains("x2 <- x1-api", Describe(FeatureBranchGraph.Build(state.Plans)));
    }

    [Fact]
    public void Base_branch_sources_round_trip_through_json()
    {
        var eventData = new FeaturePlanBaseBranchSourcesSetV1(
            X2,
            "x2",
            [FeaturePlanBranchSource.FeatureBranch, Source(X1, "x1-api")],
            Guid.Empty,
            MemoryId
        );

        var restored = System.Text.Json.JsonSerializer.Deserialize<FeaturePlanBaseBranchSourcesSetV1>(
            System.Text.Json.JsonSerializer.Serialize(eventData)
        );

        Assert.Equal(eventData.Sources.ToList(), restored.Sources.ToList());
    }

    private static List<string> Describe(
        List<(FeatureBranchKey Branch, List<FeatureBranchKey> From)> edges
    ) =>
        edges
            .Select(edge => $"{edge.Branch.BranchName} <- {string.Join(", ", edge.From.Select(Name))}")
            .ToList();

    private static string Name(FeatureBranchKey key) =>
        key.PlanId is null ? "main" : key.BranchName;

    private static FeatureStateData State(params FeaturePlan[] plans) =>
        new(FeatureId) { Plans = [.. plans] };

    private static FeaturePlan Plan(
        FeaturePlanId id,
        int day,
        FeaturePlanId? previousPlanId,
        params FeaturePlanBranch[] branches
    ) =>
        new()
        {
            Id = id,
            Title = id.ToString(),
            PreviousPlanId = previousPlanId,
            Branches = [.. branches],
            CreatedAt = DateTime.UnixEpoch.AddDays(day)
        };

    private static FeaturePlanBranch Base(string name, params FeaturePlanBranchSource[] sources) =>
        new() { Name = name, Sources = [.. sources] };

    private static FeaturePlanBranch Base(string name, FeaturePlanBranch subBranch) =>
        new() { Name = name, SubBranches = [subBranch] };

    private static FeaturePlanBranchSource Source(FeaturePlanId planId, string branchName) =>
        new(planId, branchName);

    private static EventPayload Payload(IEvent eventData) =>
        EventPayload.Create(
            EventExecutor.FromDatabaseGuid(Guid.NewGuid()),
            FeatureId,
            "features-state-machine",
            eventData
        );
}
