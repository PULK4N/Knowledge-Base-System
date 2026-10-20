using EventSourcing.Shared.Models;
using FeatureModule.Application.DTOs;
using FeatureModule.Domain;
using FeatureModule.Domain.Models;

namespace FeatureModule.MCP.Tests;

public sealed class FeatureBranchGraphFormatterTests
{
    private static readonly FeaturePlanId X1 = new(Guid.NewGuid());
    private static readonly FeaturePlanId X2 = new(Guid.NewGuid());
    private static readonly FeaturePlanId X3 = new(Guid.NewGuid());

    [Fact]
    public void Format_lists_a_branch_that_came_from_two_branches_of_one_plan()
    {
        // main ──► x1-api ──┐
        //    └───► x1-web ──┴──► x2
        var feature = Feature(
            Plan(X1, "X1", FeaturePlanStatus.InProgress, 1, null, Base("x1-api"), Base("x1-web")),
            Plan(X2, "X2", FeaturePlanStatus.NotStarted, 2, X1,
                Base("x2", Source(X1, "x1-api"), Source(X1, "x1-web")))
        );

        Assert.Equal(
            """
            main - Main line  (feature branch)
            x1-api  [X1 · In progress]  from: main
            x1-web  [X1 · In progress]  from: main
            x2  [X2 · Not started]  from: x1-api, x1-web
            """,
            FeatureBranchGraphFormatter.Format(feature)
        );
    }

    [Fact]
    public void Format_lists_a_branch_that_came_from_branches_of_different_plans()
    {
        var feature = Feature(
            Plan(X1, "X1", FeaturePlanStatus.InProgress, 1, null,
                Base("x1-api", Sub("x1-api-tests"))),
            Plan(X3, "X3", FeaturePlanStatus.InProgress, 2, null, Base("x3-web")),
            Plan(X2, "X2", FeaturePlanStatus.NotStarted, 3, X1,
                Base("x2", Source(X1, "x1-api"), Source(X3, "x3-web")))
        );

        Assert.Equal(
            """
            main - Main line  (feature branch)
            x1-api  [X1 · In progress]  from: main
            x1-api-tests  [X1 · sub-branch]  from: x1-api
            x3-web  [X3 · In progress]  from: main
            x2  [X2 · Not started]  from: x1-api, x3-web
            """,
            FeatureBranchGraphFormatter.Format(feature)
        );
    }

    [Fact]
    public void Format_leaves_out_a_completed_plan_and_joins_its_branches_to_their_sources()
    {
        // main ← x1 ← x2 shows as main ← x2 once x1 is completed.
        var feature = Feature(
            Plan(X1, "X1", FeaturePlanStatus.Completed, 1, null,
                Base("x1-api", Sub("x1-api-tests")), Base("x1-web")),
            Plan(X2, "X2", FeaturePlanStatus.InProgress, 2, X1,
                Base("x2", Source(X1, "x1-api-tests"), Source(X1, "x1-web")))
        );

        Assert.Equal(
            """
            main - Main line  (feature branch)
            x2  [X2 · In progress]  from: main
            """,
            FeatureBranchGraphFormatter.Format(feature)
        );
    }

    [Fact]
    public void Format_keeps_the_sources_of_a_completed_branch_when_only_one_source_was_completed()
    {
        // x1 is completed and x1-api came from x3-web, so x2 now comes from
        // x3-web, while its other source x3-api is still shown.
        var feature = Feature(
            Plan(X1, "X1", FeaturePlanStatus.Completed, 1, null,
                Base("x1-api", Source(X3, "x3-web"))),
            Plan(X3, "X3", FeaturePlanStatus.InProgress, 2, null, Base("x3-web"), Base("x3-api")),
            Plan(X2, "X2", FeaturePlanStatus.NotStarted, 3, null,
                Base("x2", Source(X1, "x1-api"), Source(X3, "x3-api")))
        );

        Assert.Equal(
            """
            main - Main line  (feature branch)
            x3-web  [X3 · In progress]  from: main
            x3-api  [X3 · In progress]  from: main
            x2  [X2 · Not started]  from: x3-web, x3-api
            """,
            FeatureBranchGraphFormatter.Format(feature)
        );
    }

    [Fact]
    public void Format_lists_a_branch_after_its_sources_even_when_its_plan_was_created_first()
    {
        var feature = Feature(
            Plan(X1, "X1", FeaturePlanStatus.InProgress, 1, null, Base("x1-api", Source(X3, "x3-web"))),
            Plan(X3, "X3", FeaturePlanStatus.InProgress, 2, null, Base("x3-web"))
        );

        Assert.Equal(
            """
            main - Main line  (feature branch)
            x3-web  [X3 · In progress]  from: main
            x1-api  [X1 · In progress]  from: x3-web
            """,
            FeatureBranchGraphFormatter.Format(feature)
        );
    }

    [Theory]
    [InlineData(false, null)]
    [InlineData(true, "(feature branch not set)\nx  [X1 · Not started]  from: feature branch")]
    public void Format_shows_a_missing_feature_branch_only_when_there_are_plans(
        bool hasPlan,
        string? expected
    )
    {
        var plans = hasPlan
            ? new[] { Plan(X1, "X1", FeaturePlanStatus.NotStarted, 1, null, Base("x")) }
            : [];

        Assert.Equal(
            expected,
            FeatureBranchGraphFormatter.Format(Feature(null, plans))
        );
    }

    [Fact]
    public void Plan_formatter_shows_every_base_branch_with_its_sources()
    {
        var feature = Feature(
            Plan(X1, "X1", FeaturePlanStatus.Completed, 1, null, Base("x1-api")),
            Plan(X2, "X2", FeaturePlanStatus.NotStarted, 2, X1,
                Base("x2", Sub("x2-tests")),
                Base("x2-web", Source(X1, "x1-api"), FeaturePlanBranchSource.FeatureBranch))
        );
        var plan = feature.Plans.Single(item => item.Id == X2.Value);

        Assert.Equal(
            """
            x2  (from: x1-api)
            └── x2-tests
            x2-web  (from: x1-api, main)
            """,
            FeaturePlanBranchTreeFormatter.FormatBaseBranches(plan, feature)
        );
    }

    private static FeatureDto Feature(params FeaturePlan[] plans) =>
        Feature(new FeatureBranch { Name = "main", Description = "Main line" }, plans);

    private static FeatureDto Feature(FeatureBranch? branch, FeaturePlan[] plans) =>
        FeatureDto.FromStateData(
            new FeatureStateData(AggregateId.FromDatabaseGuid(Guid.NewGuid()))
            {
                Branch = branch,
                Plans = [.. plans]
            }
        );

    private static FeaturePlan Plan(
        FeaturePlanId id,
        string title,
        FeaturePlanStatus status,
        int day,
        FeaturePlanId? previousPlanId,
        params FeaturePlanBranch[] branches
    ) =>
        new()
        {
            Id = id,
            Title = title,
            Status = status,
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
