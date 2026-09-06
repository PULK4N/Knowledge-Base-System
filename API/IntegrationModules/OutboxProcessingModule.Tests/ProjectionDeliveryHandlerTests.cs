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
    public async Task ARepeatedDeliveryRebuildsTheSameState()
    {
        var handler = CreateHandler(projections: [ nameof(RecordingProjector) ]);
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

    private const float OpeningDeposit = 100f;
    private const float DeliveredDeposit = 30f;

    /// <summary>
    /// The outbox row is written in the same transaction as the event, so the
    /// delivered event is always already part of committed history.
    /// </summary>
    private string Body()
    {
        var row = SerializedPayloadMessage.FromPayload(DeliveredPayload());
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
        IEventStore? eventStore = null
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
                definitions, new ProjectorRegistry([ _first, _second ])),
            eventStore ?? CreateEventStore(history ?? CommittedHistory()),
            CreateCalculator(definitions)
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
