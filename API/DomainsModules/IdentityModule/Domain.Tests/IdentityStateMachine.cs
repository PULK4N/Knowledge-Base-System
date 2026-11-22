using EventSourcing.Core;
using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Models;
using EventSourcing.Core.Providers;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityModule.Domain.Tests;

/// <summary>Builds the state calculator over the real identity YAML copied to the test output.</summary>
internal static class IdentityStateMachine
{
    public const string Id = "identity-state-machine";

    public static readonly EventExecutor Executor =
        EventExecutor.FromDatabaseGuid(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

    static IdentityStateMachine() =>
        new ServiceCollection().RegisterEventSourcingCore(typeof(AccountStateData).Assembly);

    public static StateCalculator Calculator()
    {
        var definitions = new YamlStateMachineDefinitionProvider(
            Path.Combine(AppContext.BaseDirectory, "StateMachines"));
        return new StateCalculator(new OrderNumberHelper(), new StateProvider(),
            new EventValidatorProvider(definitions), definitions);
    }

    public static AccountStateData Replay(AggregateId accountId, params IEvent[] events) =>
        (AccountStateData)Calculator().Calculate(Payloads(accountId, events), []).StateData;

    public static List<EventPayload> Payloads(AggregateId accountId, params IEvent[] events) =>
        events.Select((eventData, index) =>
        {
            var payload = EventPayload.Create(Executor, accountId, Id, eventData);
            payload.EventExecutionInfo.OrderNumber = (uint)index + 1;
            return payload;
        }).ToList();

    private sealed class StateProvider : IStateDataProvider
    {
        public object GetStateDataByStateMachine(string stateMachineId, AggregateId aggregateId) =>
            new AccountStateData(aggregateId);
    }
}
