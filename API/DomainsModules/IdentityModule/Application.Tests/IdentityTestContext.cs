using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Models;
using EventSourcing.Core.Providers;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using IdentityModule.Domain;
using IdentityModule.Domain.Events;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using UUIDNext;

namespace IdentityModule.Application.Tests;

/// <summary>A state machine handler over the real identity YAML and an in-memory event store.</summary>
internal sealed class IdentityTestContext : IEventStoreWithOutbox
{
    public static readonly Executor Executor = new()
    {
        Id = EventExecutor.FromDatabaseGuid(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
        IsAdministrator = true
    };

    public static readonly PasswordHasher<AccountStateData> Hasher = new();

    private readonly List<EventPayload> _history = [];

    static IdentityTestContext()
    {
        new ServiceCollection().RegisterEventSourcingCore(typeof(AccountStateData).Assembly);
        DatabaseFriendlyGuidGenerator.SetDefaultGuidGenerationDatabase(Database.PostgreSql);
    }

    public IdentityTestContext()
    {
        var definitions = new YamlStateMachineDefinitionProvider(
            Path.Combine(AppContext.BaseDirectory, "StateMachines"));
        Handler = new StateMachineHandler(new StateCalculator(new OrderNumberHelper(), new StateProvider(),
            new EventValidatorProvider(definitions), definitions), this);
    }

    public StateMachineHandler Handler { get; }

    public List<EventPayload> Written { get; } = [];

    /// <param name="password">The password the administrator has set; null keeps the seeded state.</param>
    public IdentityTestContext WithAdministrator(string? password = null)
    {
        With(IdentityDefaults.AdministratorId, new AdministratorCreatedV1(IdentityDefaults.AdministratorUserName));
        if (password is null)
            return this;

        var hash = Hasher.HashPassword(new AccountStateData(IdentityDefaults.AdministratorId), password);
        return With(IdentityDefaults.AdministratorId, new AdministratorPasswordChangedV1(hash));
    }

    public IdentityTestContext With(AggregateId accountId, params IEvent[] events)
    {
        foreach (var eventData in events)
        {
            var payload = EventPayload.Create(Executor.Id, accountId, IdentityDefaults.StateMachineId, eventData);
            payload.EventExecutionInfo.OrderNumber =
                (uint)_history.Count(value => value.EventExecutionInfo.AggregateId == accountId) + 1;
            _history.Add(payload);
        }
        return this;
    }

    public Task<Dictionary<AggregateId, List<EventPayload>>> GetEvents(List<AggregateId> aggregateIds) =>
        Task.FromResult(aggregateIds.ToDictionary(id => id, id => _history.Concat(Written)
            .Where(payload => payload.EventExecutionInfo.AggregateId == id).ToList()));

    public Task Write(Dictionary<AggregateId, StateInfo> stateInfos)
    {
        Written.AddRange(stateInfos.Values.SelectMany(state => state.LastExecutedPayloads));
        return Task.CompletedTask;
    }

    private sealed class StateProvider : IStateDataProvider
    {
        public object GetStateDataByStateMachine(string stateMachineId, AggregateId aggregateId) =>
            new AccountStateData(aggregateId);
    }
}
