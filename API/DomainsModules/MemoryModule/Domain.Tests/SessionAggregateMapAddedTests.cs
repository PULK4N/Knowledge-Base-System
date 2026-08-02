using EventSourcing.Shared.Models;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;

namespace MemoryModule.Domain.Tests;

public sealed class SessionAggregateMapAddedTests
{
    private static readonly ThreadId ThreadId =
        new(Guid.Parse("019fb72e-e0c3-7452-b32b-5bbf65433c98"));

    private static readonly AggregateId FirstMemoryAggregateId =
        AggregateId.FromDatabaseGuid(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
        );

    private static readonly AggregateId SecondMemoryAggregateId =
        AggregateId.FromDatabaseGuid(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
        );

    [Fact]
    public void Apply_NewSession_AddsMapping()
    {
        var state = new SessionAggregateMapStateData(
            MemoryAggregateIds.SessionAggregateMap
        );
        var @event = new SessionAggregateMapAddedV1(
            ThreadId,
            FirstMemoryAggregateId
        );

        @event.Apply(state, new EventExecutionInfo());

        Assert.Equal(
            FirstMemoryAggregateId,
            state.AggregateIdsBySession[ThreadId]
        );
    }

    [Fact]
    public void Apply_ExistingSession_PreservesOriginalMapping()
    {
        var state = new SessionAggregateMapStateData(
            MemoryAggregateIds.SessionAggregateMap
        )
        {
            AggregateIdsBySession =
            {
                [ThreadId] = FirstMemoryAggregateId
            }
        };
        var @event = new SessionAggregateMapAddedV1(
            ThreadId,
            SecondMemoryAggregateId
        );

        @event.Apply(state, new EventExecutionInfo());

        Assert.Equal(
            FirstMemoryAggregateId,
            state.AggregateIdsBySession[ThreadId]
        );
    }
}
