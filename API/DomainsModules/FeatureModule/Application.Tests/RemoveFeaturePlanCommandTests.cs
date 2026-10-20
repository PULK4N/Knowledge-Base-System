using ActionModule.Shared.Models;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Application.Commands;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Tests;

public sealed class RemoveFeaturePlanCommandTests
{
    private static readonly AggregateId FeatureId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly AggregateId MemoryId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly FeaturePlanId Research = new(Guid.NewGuid());
    private static readonly FeaturePlanId Implementation = new(Guid.NewGuid());
    private static readonly FeaturePlanId Retry = new(Guid.NewGuid());
    private static readonly FeaturePlanId Docs = new(Guid.NewGuid());

    [Theory]
    [MemberData(nameof(Removals))]
    public async Task Removing_a_plan_moves_its_followers_to_the_feature_branch(
        Guid removedPlanId,
        Guid[] expectedClearedPlanIds
    )
    {
        var store = new FeatureCommandEventStore(
            FeatureId,
            Added(Research, null),
            Added(Implementation, Research),
            Added(Retry, Implementation),
            Added(Docs, Implementation)
        );
        var command = new RemoveFeaturePlanCommand(store.CreateHandler())
        {
            FeatureId = FeatureId.Value,
            PlanId = removedPlanId
        };
        command.UseUserOrigin();

        await command.Execute(new Executor { Id = EventExecutor.FromDatabaseGuid(Guid.NewGuid()) });

        Assert.Equal(1, store.WriteCount);
        Assert.IsType<FeaturePlanRemovedV2>(store.Written[0].EventData);
        Assert.Equal(
            expectedClearedPlanIds.Order(),
            store.Written
                .Skip(1)
                .Select(payload => Assert.IsType<FeaturePlanPreviousPlanSetV1>(payload.EventData))
                .Select(eventData =>
                {
                    Assert.Null(eventData.PreviousPlanId);
                    return eventData.PlanId.Value;
                })
                .Order()
        );
    }

    public static IEnumerable<object[]> Removals()
    {
        yield return [Implementation.Value, new[] { Retry.Value, Docs.Value }];
        yield return [Research.Value, new[] { Implementation.Value }];
        yield return [Docs.Value, Array.Empty<Guid>()];
    }

    private static IEvent Added(FeaturePlanId planId, FeaturePlanId? previousPlanId) =>
        new FeaturePlanAddedV4(planId, "Plan", "content", FeaturePlanContentType.Markdown, "", FeaturePlanStatus.NotStarted, $"branch-{planId.Value}", "", previousPlanId, Guid.Empty, MemoryId);
}
