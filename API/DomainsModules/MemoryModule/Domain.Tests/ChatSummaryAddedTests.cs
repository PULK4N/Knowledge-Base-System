using System.Collections.Immutable;
using System.Text.Json;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;

namespace MemoryModule.Domain.Tests;

public sealed class ChatSummaryAddedTests
{
    [Fact]
    public void V1_RoundTripsAndUsesEventTimestamp()
    {
        var timestamp = new DateTime(
            2026,
            8,
            9,
            12,
            30,
            0,
            DateTimeKind.Utc
        );
        var @event = new ChatSummaryAddedV1(
            "The user added summary support to memory state."
        );
        var deserialized = Assert.IsType<ChatSummaryAddedV1>(
            JsonSerializer.Deserialize<ChatSummaryAddedV1>(
                JsonSerializer.Serialize(@event)
            )
        );
        var state = new MemoryStateData(
            AggregateId.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            )
        );

        var result = deserialized.Apply(
            state,
            new EventExecutionInfo { Timestamp = timestamp }
        );

        Assert.Same(state, result);
        Assert.Equal(
            "The user added summary support to memory state.",
            state.ChatSummary.Summary
        );
        Assert.Equal(
            timestamp,
            state.ChatSummary.SummaryTimestamp
        );
    }

    [Fact]
    public void V2_RoundTripsAndAccumulatesUniqueIdsAcrossSummaries()
    {
        var id = new MemoryEntityId(Guid.NewGuid());
        var first = new MemoryRelatedEntity(MemoryEntityType.Feature, id);
        var second = new MemoryRelatedEntity(MemoryEntityType.Skill, id);
        var state = new MemoryStateData(AggregateId.FromDatabaseGuid(Guid.NewGuid()));
        var timestamp = new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc);

        foreach (var entity in new List<MemoryRelatedEntity> { first, first, second })
        {
            var @event = new ChatSummaryAddedV2(
                entity.Type.ToString(), ImmutableHashSet.Create(entity)
            );
            var deserialized = Assert.IsType<ChatSummaryAddedV2>(
                JsonSerializer.Deserialize<ChatSummaryAddedV2>(JsonSerializer.Serialize(@event))
            );
            Assert.Same(state, deserialized.Apply(
                state, new EventExecutionInfo { Timestamp = timestamp }
            ));
        }

        Assert.Equal(second.Type.ToString(), state.ChatSummary.Summary);
        Assert.Equal(timestamp, state.ChatSummary.SummaryTimestamp);
        new ChatSummaryAddedV2("No additional changes", [])
            .Apply(state, new EventExecutionInfo { Timestamp = timestamp });
        new ChatSummaryAddedV1("Legacy summary")
            .Apply(state, new EventExecutionInfo { Timestamp = timestamp });

        var restored = Assert.IsType<MemoryStateData>(
            JsonSerializer.Deserialize<MemoryStateData>(JsonSerializer.Serialize(state))
        );
        Assert.True(restored.RelatedEntities.SetEquals([first, second]));
    }

    [Fact]
    public void LegacyState_HasEmptyRelatedEntitySets()
    {
        var state = Assert.IsType<MemoryStateData>(
            JsonSerializer.Deserialize<MemoryStateData>("{}")
        );
        Assert.Empty(state.RelatedEntities);
    }
}
