using EventSourcing.Core;
using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Models;
using EventSourcing.Core.Providers;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain;
using FeatureModule.Domain.Events;
using Shared.Interfaces;

namespace FeatureModule.Application.Tests;

/// <summary>
/// An event store holding one feature stream, built from the given events
/// after a FeatureAddedV1, that records what a command writes.
/// </summary>
internal sealed class FeatureCommandEventStore(
    AggregateId featureId,
    params IEvent[] featureEvents
) : IEventStoreWithOutbox
{
    public List<EventPayload> Written { get; private set; } = [];
    public int WriteCount { get; private set; }

    public StateMachineHandler CreateHandler() =>
        new(
            new StateCalculator(
                new OrderNumberHelper(),
                new FeatureStateProvider(),
                new NoValidators(),
                new TestStateMachineDefinitionProvider()
            ),
            this
        );

    public Task Write(Dictionary<AggregateId, StateInfo> stateInfos)
    {
        WriteCount++;
        Written = stateInfos.Values.SelectMany(stateInfo => stateInfo.LastExecutedPayloads).ToList();
        return Task.CompletedTask;
    }

    public Task<Dictionary<AggregateId, List<EventPayload>>> GetEvents(List<AggregateId> aggregateIds)
    {
        var events = new List<IEvent> { new FeatureAddedV1(AggregateId.FromDatabaseGuid(Guid.NewGuid()), "feature", "", "") }
            .Concat(featureEvents)
            .Select((eventData, index) =>
            {
                var payload = EventPayload.Create(
                    EventExecutor.FromDatabaseGuid(Guid.NewGuid()), featureId, "features-state-machine", eventData
                );
                payload.EventExecutionInfo.OrderNumber = (uint)index + 1;
                return payload;
            })
            .ToList();

        return Task.FromResult(aggregateIds.ToDictionary(
            aggregateId => aggregateId,
            aggregateId => aggregateId == featureId ? events : []
        ));
    }

    private sealed class FeatureStateProvider : IStateDataProvider
    {
        public object GetStateDataByStateMachine(string stateMachineId, AggregateId aggregateId) =>
            new FeatureStateData(aggregateId);
    }

    private sealed class NoValidators : IEventValidatorProvider
    {
        public List<IPreEventValidator> GetPreEventStateValidators(EventPayload payload) =>
            new List<IPreEventValidator>();

        public List<IPostEventValidator> GetPostEventStateValidators(EventPayload payload) =>
            new List<IPostEventValidator>();
    }
}
