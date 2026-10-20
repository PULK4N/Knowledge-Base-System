using ActionModule.Shared.Models;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Application.Commands;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Tests;

public sealed class FeatureRecordLinkCommandTests
{
    private static readonly AggregateId FeatureId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly AggregateId MemoryId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly FeaturePlanId PlanId = new(Guid.NewGuid());
    private static readonly FeaturePlanId OtherPlanId = new(Guid.NewGuid());
    private static readonly FeatureRecordId ExistingRecordId = new(Guid.NewGuid());
    private static readonly Executor Executor = new() { Id = EventExecutor.FromDatabaseGuid(Guid.NewGuid()) };

    [Fact]
    public async Task Adding_a_record_with_plans_appends_it_to_each_plan_and_lowercases_the_type()
    {
        var store = CreateStore();
        var command = new AddFeatureRecordCommand(store.CreateHandler())
        {
            FeatureId = FeatureId.Value,
            Type = " Research ",
            Title = "Finding",
            Content = "# Finding",
            PlanIds = [PlanId.Value, OtherPlanId.Value, PlanId.Value]
        };
        command.UseUserOrigin();

        await command.Execute(Executor);

        var added = Assert.IsType<FeatureRecordAddedV3>(store.Written[0].EventData);
        Assert.Equal("research", added.Type);
        var links = store.Written
            .Skip(1)
            .Select(payload => Assert.IsType<FeaturePlanRecordsSetV1>(payload.EventData))
            .ToDictionary(eventData => eventData.PlanId, eventData => eventData.RecordIds.ToList());
        Assert.Equal([ExistingRecordId, added.RecordId], links[PlanId]);
        Assert.Equal([added.RecordId], links[OtherPlanId]);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(new string[0], true)]
    [InlineData(new[] { "existing" }, true)]
    public async Task Updating_a_plan_sets_record_links_only_when_record_ids_are_given(
        string[]? recordIds,
        bool expectsLinkEvent
    )
    {
        var store = CreateStore();
        var command = new UpdateFeaturePlanCommand(store.CreateHandler())
        {
            FeatureId = FeatureId.Value,
            PlanId = PlanId.Value,
            Title = "Plan",
            Content = "content",
            RecordIds = recordIds?.Select(_ => ExistingRecordId.Value).ToList()
        };
        command.UseUserOrigin();

        await command.Execute(Executor);

        Assert.IsType<FeaturePlanUpdatedV1>(store.Written[0].EventData);
        var link = store.Written
            .Skip(1)
            .Select(payload => payload.EventData)
            .OfType<FeaturePlanRecordsSetV1>()
            .SingleOrDefault();
        Assert.Equal(expectsLinkEvent, link != default);
        if (expectsLinkEvent)
            Assert.Equal(recordIds!.Length, link.RecordIds.Length);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Adding_a_plan_links_records_only_when_given(bool withRecords)
    {
        var store = CreateStore();
        var command = new AddFeaturePlanCommand(store.CreateHandler())
        {
            FeatureId = FeatureId.Value,
            Title = "Plan",
            Content = "content",
            BranchName = "feature/new-plan",
            RecordIds = withRecords ? [ExistingRecordId.Value] : null
        };
        command.UseUserOrigin();

        await command.Execute(Executor);

        var added = Assert.IsType<FeaturePlanAddedV4>(store.Written[0].EventData);
        Assert.Equal(
            withRecords ? [ExistingRecordId] : [],
            store.Written
                .Skip(1)
                .Select(payload => Assert.IsType<FeaturePlanRecordsSetV1>(payload.EventData))
                .Where(eventData => eventData.PlanId == added.PlanId)
                .SelectMany(eventData => eventData.RecordIds)
                .ToList()
        );
    }

    [Theory]
    [InlineData("", "Title", "Content", true)]
    [InlineData("research", " ", "Content", true)]
    [InlineData("research", "Title", "", true)]
    [InlineData("research", "Title", "Content", false)]
    public async Task Record_add_rejects_blank_type_title_or_content_and_empty_plan_ids(
        string type,
        string title,
        string content,
        bool expectedRejected
    )
    {
        var command = new AddFeatureRecordCommand(null!)
        {
            FeatureId = FeatureId.Value,
            Type = type,
            Title = title,
            Content = content
        };

        Assert.Equal(!expectedRejected, await command.CanExecute(Executor));

        command.Type = "research";
        command.Title = "Title";
        command.Content = "Content";
        command.PlanIds = [Guid.Empty];
        Assert.False(await command.CanExecute(Executor));
    }

    private static FeatureCommandEventStore CreateStore() =>
        new(
            FeatureId,
            new IEvent[]
            {
                new FeatureRecordAddedV3(ExistingRecordId, "conversation", "Existing", "content", null, "", Guid.Empty, MemoryId),
                PlanAdded(PlanId),
                PlanAdded(OtherPlanId),
                new FeaturePlanRecordsSetV1(PlanId, [ExistingRecordId], Guid.Empty, MemoryId)
            }
        );

    private static IEvent PlanAdded(FeaturePlanId planId) =>
        new FeaturePlanAddedV4(planId, "Plan", "content", FeaturePlanContentType.Markdown, "", FeaturePlanStatus.NotStarted, $"branch-{planId.Value}", "", null, Guid.Empty, MemoryId);
}
