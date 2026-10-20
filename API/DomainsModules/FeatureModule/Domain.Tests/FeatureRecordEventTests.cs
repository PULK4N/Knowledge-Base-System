using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using FeatureModule.Domain.Validators;

namespace FeatureModule.Domain.Tests;

public sealed class FeatureRecordEventTests
{
    private static readonly AggregateId FeatureId =
        AggregateId.FromDatabaseGuid(Guid.NewGuid());

    private static readonly AggregateId MemoryId =
        AggregateId.FromDatabaseGuid(Guid.NewGuid());

    private static readonly FeatureRecordId RecordId = new(Guid.NewGuid());

    private static readonly FeatureRecordId OtherRecordId = new(Guid.NewGuid());

    private static readonly FeaturePlanId PlanId = new(Guid.NewGuid());

    private static readonly EventExecutionInfo ExecutionInfo = new()
    {
        AggregateId = FeatureId,
        Timestamp = DateTime.UnixEpoch
    };

    [Fact]
    public void RecordV3_events_set_type_title_content_and_source()
    {
        var state = new FeatureStateData(FeatureId);

        new FeatureRecordAddedV3(
            RecordId,
            "research",
            "Event store",
            "# Findings",
            FeatureRecordSourceType.Web,
            "https://example.com",
            Guid.NewGuid(),
            MemoryId
        ).Apply(state, ExecutionInfo);
        new FeatureRecordUpdatedV3(
            RecordId,
            "decision",
            "Event store choice",
            "Use PostgreSQL.",
            null,
            string.Empty,
            Guid.NewGuid(),
            MemoryId
        ).Apply(state, ExecutionInfo);

        var record = Assert.Single(state.Records);
        Assert.Equal("decision", record.Type);
        Assert.Equal("Event store choice", record.Title);
        Assert.Equal("Use PostgreSQL.", record.Content);
        Assert.Null(record.SourceType);
        Assert.Equal(2, state.MemoryHistory.Count);
    }

    [Theory]
    [InlineData("## Why keep plans?\nMore text", "Why keep plans?")]
    [InlineData("\n\n  plain question  ", "plain question")]
    [InlineData("   ", "Conversation")]
    public void Conversation_records_take_a_title_from_the_user_message(
        string userMessage,
        string expectedTitle
    )
    {
        var state = new FeatureStateData(FeatureId);

        new FeatureRecordAddedV1(RecordId, userMessage, "Answer")
            .Apply(state, ExecutionInfo);

        var record = Assert.Single(state.Records);
        Assert.Equal(FeatureRecordTypes.Conversation, record.Type);
        Assert.Equal(expectedTitle, record.Title);
        Assert.EndsWith("## AI\n\nAnswer", record.Content);
    }

    [Fact]
    public void Conversation_titles_are_cut_to_eighty_characters()
    {
        var state = new FeatureStateData(FeatureId);

        new FeatureRecordAddedV1(RecordId, new string('a', 100), "Answer")
            .Apply(state, ExecutionInfo);

        Assert.Equal(new string('a', 80) + "…", state.Records[0].Title);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Removing_a_record_removes_it_from_every_plan(
        bool removeAsResearchDiscovery
    )
    {
        var state = CreateStateWithPlan();
        new FeaturePlanRecordsSetV1(
            PlanId,
            [RecordId, OtherRecordId],
            Guid.NewGuid(),
            MemoryId
        ).Apply(state, ExecutionInfo);

        IEvent removal = removeAsResearchDiscovery
            ? new FeatureResearchDiscoveryRemovedV1(
                FeatureResearchDiscoveryId.FromDatabaseGuid(RecordId.Value)
            )
            : new FeatureRecordRemovedV2(RecordId, Guid.NewGuid(), MemoryId);
        removal.Apply(state, ExecutionInfo);

        Assert.Equal(OtherRecordId, Assert.Single(state.Plans[0].RecordIds));
        Assert.Equal(OtherRecordId, Assert.Single(state.Records).Id);
    }

    [Fact]
    public void PlanRecordsSet_replaces_the_links()
    {
        var state = CreateStateWithPlan();

        new FeaturePlanRecordsSetV1(PlanId, [RecordId], Guid.NewGuid(), MemoryId)
            .Apply(state, ExecutionInfo);
        new FeaturePlanRecordsSetV1(PlanId, [], Guid.NewGuid(), MemoryId)
            .Apply(state, ExecutionInfo);

        Assert.Empty(state.Plans[0].RecordIds);
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public void Plan_links_require_existing_distinct_records(
        bool linksMissingRecord,
        bool linksRecordTwice,
        bool expectedSuccess
    )
    {
        List<FeatureRecordId> recordIds = linksMissingRecord
            ? [RecordId, new FeatureRecordId(Guid.NewGuid())]
            : linksRecordTwice
                ? [RecordId, RecordId]
                : [RecordId, OtherRecordId];
        var payload = EventPayload.Create(
            EventExecutor.FromDatabaseGuid(Guid.NewGuid()),
            FeatureId,
            "features-state-machine",
            new FeaturePlanRecordsSetV1(
                PlanId,
                [.. recordIds],
                Guid.NewGuid(),
                MemoryId
            )
        );

        var result = new FeaturePlanRecordsMustExistValidator()
            .Validate(CreateStateWithPlan(), payload);

        Assert.Equal(expectedSuccess, result.Succeded);
    }

    [Fact]
    public void PlanRecordsSet_round_trips_through_json()
    {
        var eventData = new FeaturePlanRecordsSetV1(
            PlanId,
            [RecordId, OtherRecordId],
            Guid.NewGuid(),
            MemoryId
        );

        var json = System.Text.Json.JsonSerializer.Serialize(eventData);
        var restored = System.Text.Json.JsonSerializer
            .Deserialize<FeaturePlanRecordsSetV1>(json);

        Assert.Equal(eventData.RecordIds.ToList(), restored.RecordIds.ToList());
        Assert.Equal(PlanId, restored.PlanId);
    }

    private static FeatureStateData CreateStateWithPlan() =>
        new(FeatureId)
        {
            Records =
            [
                new FeatureRecord { Id = RecordId },
                new FeatureRecord { Id = OtherRecordId }
            ],
            Plans = [new FeaturePlan { Id = PlanId }]
        };
}
