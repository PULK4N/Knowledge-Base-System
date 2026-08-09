using System.Text.Json;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Events;

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
}
