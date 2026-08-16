using AdministrationModule.Persistence;
using EventSourcing.Persistence;
using EventSourcing.Persistence.Models;
using EventSourcing.Shared.Containers;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AdministrationModule.Persistence.Tests;

public sealed class ProjectionReplayRepositoryTests
{
    [Fact]
    public async Task GetLastEvents_returns_only_latest_event_per_aggregate()
    {
        EventTypeContainer.AddEventType(
            typeof(ProjectionReplayRepositoryTestEvent)
        );
        var options = new DbContextOptionsBuilder<
            EventSourcingDbContext
        >()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var dbContext = new EventSourcingDbContext(
            options
        );
        var expectedFirst = CreatePayload(
            "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            "target-state-machine",
            2
        );
        var expectedSecond = CreatePayload(
            "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
            "target-state-machine",
            1
        );
        dbContext.SerializedEventPayload.AddRange(
            SerializedEventPayload.FromPayload(
                CreatePayload(
                    "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                    "target-state-machine",
                    1
                )
            ),
            SerializedEventPayload.FromPayload(expectedFirst),
            SerializedEventPayload.FromPayload(expectedSecond),
            SerializedEventPayload.FromPayload(
                CreatePayload(
                    "cccccccc-cccc-cccc-cccc-cccccccccccc",
                    "other-state-machine",
                    1
                )
            )
        );
        await dbContext.SaveChangesAsync();
        var repository = new ProjectionReplayRepository(dbContext);

        var events = await repository.GetLastEvents(
            "target-state-machine"
        );

        Assert.Equal(2, events.Count);
        Assert.Collection(
            events,
            payload => Assert.Equal(
                expectedFirst.EventExecutionInfo.AggregateId,
                payload.EventExecutionInfo.AggregateId
            ),
            payload => Assert.Equal(
                expectedSecond.EventExecutionInfo.AggregateId,
                payload.EventExecutionInfo.AggregateId
            )
        );
        Assert.Equal(
            [2u, 1u],
            events
                .Select(
                    payload =>
                        payload.EventExecutionInfo.OrderNumber
                )
                .ToList()
        );
    }

    private static EventPayload CreatePayload(
        string aggregateId,
        string stateMachineId,
        uint orderNumber
    )
    {
        var payload = EventPayload.Create(
            EventExecutor.FromDatabaseGuid(
                Guid.Parse(
                    "11111111-1111-1111-1111-111111111111"
                )
            ),
            AggregateId.FromDatabaseGuid(
                Guid.Parse(aggregateId)
            ),
            stateMachineId,
            new ProjectionReplayRepositoryTestEvent()
        );
        payload.EventExecutionInfo.OrderNumber = orderNumber;

        return payload;
    }

    private sealed class ProjectionReplayRepositoryTestEvent : IEvent
    {
        public object Apply(
            object stateData,
            EventExecutionInfo eventExecutionInfo
        ) =>
            stateData;
    }
}
