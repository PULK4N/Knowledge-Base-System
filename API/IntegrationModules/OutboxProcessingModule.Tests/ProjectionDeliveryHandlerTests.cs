using System.Text.Json;
using EventSourcing.Core;
using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Models;
using EventSourcing.Core.Providers;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Persistence.Models;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using SharedModule.DistributedMessaging.Projections;
using Moq;
using OutboxProcessingModule.Application;
using OutboxProcessingModule.Tests.TestModels;
using SharedModule.DistributedMessaging.Queues;

namespace OutboxProcessingModule.Tests;

[Collection(EventTypeCollection.Name)]
public sealed class ProjectionDeliveryHandlerTests
{
    private const string StateMachineId = "accounts-state-machine";
    private const string EventName = nameof(MoneyDeposited);

    private readonly AggregateId _aggregateId = new(Guid.NewGuid());
    private readonly RecordingProjector _first = new();
    private readonly SecondRecordingProjector _second = new();
    private readonly FailingProjector _failing = new();

    [Fact]
    public void TheHandlerOnlyServesTheProjectionsRole() =>
        Assert.Equal(DeliveryRole.Projections, CreateHandler().Role);

    [Fact]
    public async Task EveryProjectorNamedInYamlRunsOnStateRebuiltFromHistory()
    {
        var handler = CreateHandler(
            projections: [ nameof(RecordingProjector), nameof(SecondRecordingProjector) ]);

        await handler.Handle(Body(), CancellationToken.None);

        var state = Assert.Single(_first.Received);
        Assert.Equal(_aggregateId, state.AggregateId);
        Assert.Equal(
            OpeningDeposit + DeliveredDeposit,
            ((AccountStateData)state.StateData).Money);
        Assert.Single(_second.Received);
    }

    [Fact]
    public async Task ARepeatedDeliveryRebuildsTheSameStateWithoutACheckpoint()
    {
        var checkpoints = new Mock<IProjectionCheckpointCache>();
        checkpoints.Setup(cache => cache.Get(
                It.IsAny<string>(), It.IsAny<AggregateId>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectionCheckpoint("uncached", null));
        var handler = CreateHandler(projections: [ nameof(RecordingProjector) ], checkpoints: checkpoints.Object);
        var body = Body();

        await handler.Handle(body, CancellationToken.None);
        await handler.Handle(body, CancellationToken.None);

        Assert.Equal(2, _first.Received.Count);
        Assert.Equal(
            ((AccountStateData)_first.Received[0].StateData).Money,
            ((AccountStateData)_first.Received[1].StateData).Money);
    }

    [Fact]
    public async Task TheDeliveredPayloadIsTheOnlyProjectorContext()
    {
        var handler = CreateHandler(projections: [ nameof(RecordingProjector) ]);

        await handler.Handle(Body(), CancellationToken.None);

        var payload = Assert.Single(Assert.Single(_first.Received).LastExecutedPayloads);
        Assert.Equal(EventName, payload.EventExecutionInfo.EventName);
        Assert.Equal(_aggregateId, payload.EventExecutionInfo.AggregateId);
    }

    [Fact]
    public async Task NothingIsReadWhenTheStateMachineDeclaresNoProjection()
    {
        var eventStore = new Mock<IEventStore>(MockBehavior.Strict);
        var handler = CreateHandler(projections: [ ], eventStore: eventStore.Object);

        await handler.Handle(Body(), CancellationToken.None);

        eventStore.Verify(
            store => store.GetEvents(It.IsAny<List<AggregateId>>()), Times.Never);
    }

    [Fact]
    public async Task AnAggregateWithoutCommittedHistoryFails()
    {
        var handler = CreateHandler(
            projections: [ nameof(RecordingProjector) ], history: [ ]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(Body(), CancellationToken.None));
    }

    [Fact]
    public async Task ABodyThatIsNotAnOutboxRowFails()
    {
        var handler = CreateHandler(projections: [ nameof(RecordingProjector) ]);

        await Assert.ThrowsAnyAsync<Exception>(
            () => handler.Handle("null", CancellationToken.None));
    }

    [Theory]
    [InlineData(1u)]
    [InlineData(2u)]
    [InlineData(3u)]
    public async Task CompletedSnapshotSkipsOlderOrEqualDeliveriesWithoutReadingHistory(uint deliveredOrder)
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var checkpoints = new LocalProjectionCheckpointCache(memory);
        var history = CommittedHistory();
        history.Add(Payload(10, 3));
        var store = Mock.Get(CreateEventStore(history));
        var handler = CreateHandler(
            projections: [nameof(RecordingProjector)], eventStore: store.Object, checkpoints: checkpoints);

        await handler.Handle(Body(1), CancellationToken.None);
        // A fresh delivery scope still sees the same completed checkpoint.
        await CreateHandler(
            projections: [nameof(RecordingProjector)], eventStore: store.Object, checkpoints: checkpoints
        ).Handle(Body(deliveredOrder), CancellationToken.None);

        Assert.Single(_first.Received);
        Assert.Equal(3u, _first.Received[0].CurrentOrderNumber);
        store.Verify(value => value.GetEvents(It.IsAny<List<AggregateId>>()), Times.Once);
    }

    [Fact]
    public async Task ANewerEventRebuildsAndAdvancesTheCheckpoint()
    {
        var history = CommittedHistory();
        var handler = CreateHandler(projections: [nameof(RecordingProjector)], history: history);
        await handler.Handle(Body(), CancellationToken.None);
        history.Add(Payload(10, 3));

        await handler.Handle(Body(3), CancellationToken.None);
        await handler.Handle(Body(3), CancellationToken.None);

        Assert.Equal(2, _first.Received.Count);
        Assert.Equal(3u, _first.Received[1].CurrentOrderNumber);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PartialFailureOrCancellationDoesNotAdvanceTheCheckpoint(bool cancelled)
    {
        _failing.Failure = cancelled
            ? new OperationCanceledException()
            : new InvalidOperationException("Projection failed");
        var handler = CreateHandler(projections: [nameof(RecordingProjector), nameof(FailingProjector)]);

        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(Body(), CancellationToken.None));
        _failing.Failure = null;
        await handler.Handle(Body(), CancellationToken.None);
        await handler.Handle(Body(), CancellationToken.None);

        Assert.Equal(2, _first.Received.Count);
        Assert.Equal(2, _failing.Calls);
    }

    [Fact]
    public async Task ChangedProjectorSelectionAndManualReplayAreNotSkipped()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var checkpoints = new LocalProjectionCheckpointCache(memory);
        await CreateHandler(projections: [nameof(RecordingProjector)], checkpoints: checkpoints)
            .Handle(Body(), CancellationToken.None);
        var handler = CreateHandler(
            projections: [nameof(RecordingProjector), nameof(SecondRecordingProjector)], checkpoints: checkpoints);

        await handler.Handle(Body(), CancellationToken.None);
        await checkpoints.Invalidate(StateMachineId);
        await handler.Handle(Body(), CancellationToken.None);

        Assert.Equal(3, _first.Received.Count);
        Assert.Equal(2, _second.Received.Count);
    }

    private sealed class FailingProjector : IProjector
    {
        public Exception? Failure { get; set; }
        public int Calls { get; private set; }

        public Task Update(List<StateInfo> states)
        {
            Calls++;
            return Failure is null ? Task.CompletedTask : Task.FromException(Failure);
        }
    }

    private const float OpeningDeposit = 100f;
    private const float DeliveredDeposit = 30f;

    /// <summary>
    /// The outbox row is written in the same transaction as the event, so the
    /// delivered event is always already part of committed history.
    /// </summary>
    private string Body(uint orderNumber = 2)
    {
        var row = SerializedPayloadMessage.FromPayload(Payload(DeliveredDeposit, orderNumber));
        row.Id = 7;

        return JsonSerializer.Serialize(row);
    }

    private EventPayload DeliveredPayload() => Payload(DeliveredDeposit, orderNumber: 2);

    private List<EventPayload> CommittedHistory() =>
        [ Payload(OpeningDeposit, orderNumber: 1), DeliveredPayload() ];

    private EventPayload Payload(float amount, uint orderNumber) => new()
    {
        EventData = new MoneyDeposited { Amount = amount },
        EventExecutionInfo = new EventExecutionInfo
        {
            AggregateId = _aggregateId,
            EventName = EventName,
            StateMachineId = StateMachineId,
            NewState = "Open",
            OrderNumber = orderNumber,
            Timestamp = DateTime.UtcNow,
            EventExecutor = EventExecutor.FromDatabaseGuid(Guid.NewGuid())
        }
    };

    private ProjectionDeliveryHandler CreateHandler(
        List<string>? projections = null,
        List<EventPayload>? history = null,
        IEventStore? eventStore = null,
        IProjectionCheckpointCache? checkpoints = null
    )
    {
        var definition = new StateMachineDefinition
        {
            Id = StateMachineId,
            InitializationEvents = [ EventName ],
            Projections = projections ?? [ ]
        };

        var definitions = new TestDefinitionProvider(definition);

        return new ProjectionDeliveryHandler(
            new ProjectionSelector(
                definitions, new ProjectorRegistry([ _first, _second, _failing ])),
            eventStore ?? CreateEventStore(history ?? CommittedHistory()),
            CreateCalculator(definitions),
            checkpoints ?? new LocalProjectionCheckpointCache(new MemoryCache(new MemoryCacheOptions()))
        );
    }

    private IEventStore CreateEventStore(List<EventPayload> history)
    {
        var eventStore = new Mock<IEventStore>();
        eventStore
            .Setup(store => store.GetEvents(It.IsAny<List<AggregateId>>()))
            .ReturnsAsync(
                history.Count == 0
                    ? new Dictionary<AggregateId, List<EventPayload>>()
                    : new Dictionary<AggregateId, List<EventPayload>>
                    {
                        [_aggregateId] = history
                    });

        return eventStore.Object;
    }

    private StateCalculator CreateCalculator(IStateMachineDefinitionProvider definitions)
    {
        var stateData = new Mock<IStateDataProvider>();
        stateData
            .Setup(provider => provider.GetStateDataByStateMachine(
                StateMachineId, It.IsAny<AggregateId>()))
            .ReturnsAsync(
                (string _, AggregateId aggregateId) => new AccountStateData(aggregateId));

        var validators = new Mock<IEventValidatorProvider>();
        validators
            .Setup(provider => provider.GetPreEventStateValidators(It.IsAny<EventPayload>()))
            .ReturnsAsync([ ]);
        validators
            .Setup(provider => provider.GetPostEventStateValidators(It.IsAny<EventPayload>()))
            .ReturnsAsync([ ]);

        var constraints = new Mock<IUniqueEventConstraintProvider>();
        constraints
            .Setup(provider => provider.GetConstraintsToAdd(
                It.IsAny<object>(), It.IsAny<EventPayload>()))
            .Returns([ ]);
        constraints
            .Setup(provider => provider.GetConstraintsToRemove(
                It.IsAny<object>(), It.IsAny<EventPayload>()))
            .Returns([ ]);

        return new StateCalculator(
            new OrderNumberHelper(),
            stateData.Object,
            validators.Object,
            constraints.Object,
            definitions
        );
    }
}

/// <summary>
/// The event and state data type containers are static, so the tests that need
/// them registered must not run beside tests that register others.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class EventTypeCollection : ICollectionFixture<EventTypeFixture>
{
    public const string Name = "Event type containers";
}

public sealed class EventTypeFixture
{
    public EventTypeFixture()
    {
        var services = new ServiceCollection();
        var testAssembly = typeof(EventTypeFixture).Assembly;

        services.RegisterStateDataTypes(testAssembly);
        services.RegisterEventTypes(testAssembly);
    }
}
