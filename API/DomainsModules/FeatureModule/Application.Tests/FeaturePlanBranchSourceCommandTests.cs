using ActionModule.Shared.Models;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Application.Commands;
using FeatureModule.Application.DTOs;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Tests;

public sealed class FeaturePlanBranchSourceCommandTests
{
    private static readonly AggregateId FeatureId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly AggregateId MemoryId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly FeaturePlanId X1 = new(Guid.NewGuid());
    private static readonly FeaturePlanId X2 = new(Guid.NewGuid());
    private static readonly Executor Executor = new() { Id = EventExecutor.FromDatabaseGuid(Guid.NewGuid()) };

    private static readonly List<FeaturePlanBranchSourceDto> Sources =
    [
        new(X1.Value, " x1-api "),
        new(null, "ignored")
    ];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Adding_a_base_branch_sets_its_sources_only_when_given(bool withSources)
    {
        var store = CreateStore();
        var command = new AddFeaturePlanBranchCommand(store.CreateHandler())
        {
            FeatureId = FeatureId.Value,
            PlanId = X2.Value,
            BranchName = " x2-web ",
            Sources = withSources ? Sources : []
        };
        command.UseUserOrigin();

        await command.Execute(Executor);

        var added = Assert.IsType<FeaturePlanBranchAddedV1>(store.Written[0].EventData);
        Assert.Null(added.ParentBranchName);
        Assert.Equal(
            withSources
                ? [new FeaturePlanBranchSource(X1, "x1-api"), FeaturePlanBranchSource.FeatureBranch]
                : [],
            store.Written
                .Skip(1)
                .Select(payload => Assert.IsType<FeaturePlanBaseBranchSourcesSetV1>(payload.EventData))
                .SelectMany(eventData =>
                {
                    Assert.Equal("x2-web", eventData.BranchName);
                    return eventData.Sources;
                })
                .ToList()
        );
    }

    [Fact]
    public async Task Sources_are_rejected_for_a_sub_branch()
    {
        var command = new AddFeaturePlanBranchCommand(null!)
        {
            FeatureId = FeatureId.Value,
            PlanId = X2.Value,
            BranchName = "x2-tests",
            ParentBranchName = "x2",
            Sources = Sources
        };

        Assert.False(await command.CanExecute(Executor));
    }

    [Fact]
    public async Task Adding_a_plan_with_branch_sources_sets_them_on_its_base_branch()
    {
        var store = CreateStore();
        var command = new AddFeaturePlanCommand(store.CreateHandler())
        {
            FeatureId = FeatureId.Value,
            Title = "X3",
            Content = "content",
            BranchName = " x3 ",
            BranchSources = Sources
        };
        command.UseUserOrigin();

        await command.Execute(Executor);

        var added = Assert.IsType<FeaturePlanAddedV4>(store.Written[0].EventData);
        var sources = Assert.IsType<FeaturePlanBaseBranchSourcesSetV1>(
            Assert.Single(store.Written.Skip(1)).EventData
        );
        Assert.Equal((added.PlanId, "x3"), (sources.PlanId, sources.BranchName));
        Assert.Equal(2, sources.Sources.Length);
    }

    [Fact]
    public async Task Setting_sources_replaces_them_with_one_event()
    {
        var store = CreateStore();
        var command = new SetFeaturePlanBranchSourcesCommand(store.CreateHandler())
        {
            FeatureId = FeatureId.Value,
            PlanId = X2.Value,
            BranchName = "x2",
            Sources = []
        };
        command.UseUserOrigin();

        await command.Execute(Executor);

        var eventData = Assert.IsType<FeaturePlanBaseBranchSourcesSetV1>(
            Assert.Single(store.Written).EventData
        );
        Assert.Empty(eventData.Sources);
    }

    [Theory]
    [InlineData(FeaturePlanStatus.Completed, null, "x1-api")]
    [InlineData(FeaturePlanStatus.Completed, "main", "")]
    [InlineData(FeaturePlanStatus.InProgress, null, null)]
    public async Task Completing_a_plan_moves_plans_that_came_from_it_to_where_it_was_merged(
        FeaturePlanStatus status,
        string? mergedInto,
        string? expectedSource
    )
    {
        // x1-api <- x2 <- x3: x3 has no sources and comes from x2 as its previous plan.
        var x3 = new FeaturePlanId(Guid.NewGuid());
        var store = new FeatureCommandEventStore(
            FeatureId,
            PlanAdded(X1, "x1-api", null),
            PlanAdded(X2, "x2", X1),
            PlanAdded(x3, "x3", X2)
        );
        var command = new ChangeFeaturePlanStatusCommand(store.CreateHandler())
        {
            FeatureId = FeatureId.Value,
            PlanId = X2.Value,
            Status = status,
            MergedInto = mergedInto is null ? [] : [new FeaturePlanBranchMergeDto("x2", null, "")]
        };
        command.UseUserOrigin();

        await command.Execute(Executor);

        Assert.IsType<FeaturePlanStatusChangedV1>(store.Written[0].EventData);
        var rewrite = store.Written
            .Skip(1)
            .Select(payload => Assert.IsType<FeaturePlanBaseBranchSourcesSetV1>(payload.EventData))
            .SingleOrDefault();
        if (expectedSource is null)
        {
            Assert.Equal(default, rewrite);
            return;
        }

        Assert.Equal((x3, "x3"), (rewrite.PlanId, rewrite.BranchName));
        Assert.Equal(expectedSource, Assert.Single(rewrite.Sources).BranchName);
    }

    [Fact]
    public async Task Merge_targets_are_rejected_unless_the_plan_is_completed()
    {
        var command = new ChangeFeaturePlanStatusCommand(null!)
        {
            FeatureId = FeatureId.Value,
            PlanId = X2.Value,
            Status = FeaturePlanStatus.InProgress,
            MergedInto = [new FeaturePlanBranchMergeDto("x2", X1.Value, "x1-api")]
        };

        Assert.False(await command.CanExecute(Executor));
    }

    private static FeatureCommandEventStore CreateStore() =>
        new(
            FeatureId,
            new IEvent[]
            {
                PlanAdded(X1, "x1-api", null),
                PlanAdded(X2, "x2", X1)
            }
        );

    private static IEvent PlanAdded(FeaturePlanId planId, string branchName, FeaturePlanId? previousPlanId) =>
        new FeaturePlanAddedV4(planId, "Plan", "content", FeaturePlanContentType.Markdown, "", FeaturePlanStatus.NotStarted, branchName, "", previousPlanId, Guid.Empty, MemoryId);
}
