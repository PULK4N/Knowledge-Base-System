using ActionModule.Shared.Models;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Application.Commands;
using FeatureModule.Application.DTOs;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Tests;

public sealed class FeaturePlanCompletionCommandTests
{
    private static readonly AggregateId FeatureId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly AggregateId MemoryId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly FeaturePlanId X1 = new(Guid.NewGuid());
    private static readonly FeaturePlanId X2 = new(Guid.NewGuid());
    private static readonly FeaturePlanId X3 = new(Guid.NewGuid());
    private static readonly Executor Executor = new() { Id = EventExecutor.FromDatabaseGuid(Guid.NewGuid()) };

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Completion_uses_the_selected_target_in_one_write_or_rejects_the_entire_write(
        bool updateContent,
        bool invalidTarget
    )
    {
        var store = new FeatureCommandEventStore(
            FeatureId,
            Added(X1, "x1-api", null),
            new FeaturePlanBranchAddedV1(X1, "x1-web", "", null, Guid.Empty, MemoryId),
            Added(X2, "x2", X1),
            Added(X3, "x3", X2)
        );
        var mergedInto = new List<FeaturePlanBranchMergeDto>
        {
            new(" x2 ", X1.Value, invalidTarget ? "missing" : " x1-web ")
        };
        FeatureCommand command = updateContent
            ? new UpdateFeaturePlanCommand(store.CreateHandler())
            {
                FeatureId = FeatureId.Value,
                PlanId = X2.Value,
                Title = "Updated plan",
                Content = "Updated content",
                Status = FeaturePlanStatus.Completed,
                MergedInto = mergedInto
            }
            : new ChangeFeaturePlanStatusCommand(store.CreateHandler())
            {
                FeatureId = FeatureId.Value,
                PlanId = X2.Value,
                Status = FeaturePlanStatus.Completed,
                MergedInto = mergedInto
            };
        command.UseUserOrigin();

        if (invalidTarget)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => command.Execute(Executor));
            Assert.Equal(0, store.WriteCount);
            Assert.Empty(store.Written);
            return;
        }

        await command.Execute(Executor);

        Assert.Equal(1, store.WriteCount);
        Assert.Equal(2, store.Written.Count);
        Assert.Equal(
            updateContent ? typeof(FeaturePlanUpdatedV1) : typeof(FeaturePlanStatusChangedV1),
            store.Written[0].EventData.GetType()
        );
        var rewrite = Assert.IsType<FeaturePlanBaseBranchSourcesSetV1>(store.Written[1].EventData);
        Assert.Equal((X3, "x3"), (rewrite.PlanId, rewrite.BranchName));
        Assert.Equal(new FeaturePlanBranchSource(X1, "x1-web"), Assert.Single(rewrite.Sources));
        Assert.Equal(
            store.Written[0].EventExecutionInfo.OrderNumber + 1,
            store.Written[1].EventExecutionInfo.OrderNumber
        );
    }

    private static IEvent Added(FeaturePlanId planId, string branchName, FeaturePlanId? previous) =>
        new FeaturePlanAddedV4(planId, "Plan", "content", FeaturePlanContentType.Markdown, "",
            FeaturePlanStatus.NotStarted, branchName, "", previous, Guid.Empty, MemoryId);
}
